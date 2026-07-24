using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BlueSandsLMS.Api.Infrastructure;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BlueSandsLMS.Api.Realtime
{
    public static class SessionSimulationSocketEndpointExtensions
    {
        private static readonly JsonSerializerOptions SocketJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static void MapSessionSimulationSocketEndpoint(this WebApplication app)
        {
            app.Map("/session/{sessionId:guid}/sim", async context =>
            {
                var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("SessionSimulationSocket");
                var store = context.RequestServices.GetRequiredService<SessionSimulationSocketStore>();
                var limiter = context.RequestServices.GetRequiredService<PerIpRateLimitService>();
                var env = context.RequestServices.GetRequiredService<IHostEnvironment>();

                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                if (env.IsProduction() && !context.Request.IsHttps)
                {
                    await RejectSocketAsync(context, "FORBIDDEN", "WSS is required in production.", logger, closeCode: 4003);
                    return;
                }

                if (!limiter.TryConsumeWebSocketConnect(context, out var retryAfter))
                {
                    var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    await RejectSocketAsync(
                        context,
                        "RATE_LIMITED",
                        $"Too many websocket connection attempts. Retry after {retryAfterSeconds} seconds.",
                        logger,
                        closeCode: 4008);
                    return;
                }

                if (!TryReadSessionId(context, out var sessionId))
                {
                    await RejectSocketAsync(context, "AUTH_REQUIRED", "Session invalid or expired", logger);
                    return;
                }

                var principal = ValidateAccessToken(context, out var tokenError);
                if (principal == null)
                {
                    await RejectSocketAsync(context, "AUTH_REQUIRED", tokenError ?? "Session invalid or expired", logger);
                    return;
                }

                var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? principal.FindFirst("role")?.Value;
                if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                {
                    await RejectSocketAsync(context, "AUTH_REQUIRED", "Student access required.", logger);
                    return;
                }

                var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? principal.FindFirst("sub")?.Value
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(sub, out var userId))
                {
                    await RejectSocketAsync(context, "AUTH_REQUIRED", "Session invalid or expired", logger);
                    return;
                }

                StudentIlsSession? session;
                await using (var scope = context.RequestServices.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<BlueSandsLMSDbContext>();
                    session = await db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == sessionId, context.RequestAborted);
                }

                if (session == null || session.StudentId != userId)
                {
                    await RejectSocketAsync(context, "AUTH_REQUIRED", "Session invalid or expired", logger);
                    return;
                }

                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await store.RegisterAsync(sessionId, socket, logger, context.RequestAborted);
                logger.LogInformation("WS connected: sessionId={SessionId}, userId={UserId}", sessionId, userId);

                try
                {
                    var lastState = store.GetLastState(sessionId) ?? session.LastSimulationStateJson;
                    if (!string.IsNullOrWhiteSpace(lastState))
                        await SendTextAsync(socket, lastState, context.RequestAborted);

                    while (socket.State == WebSocketState.Open)
                    {
                        using var inactivityCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                        inactivityCts.CancelAfter(TimeSpan.FromMinutes(10));

                        var frameJson = await ReceiveTextAsync(socket, inactivityCts.Token);
                        if (frameJson == null) break;

                        SocketVariablesFrame? frame;
                        try
                        {
                            frame = JsonSerializer.Deserialize<SocketVariablesFrame>(frameJson, SocketJsonOptions);
                        }
                        catch
                        {
                            await SendErrorFrameAsync(socket, "VALIDATION_ERROR", "Invalid frame payload.", context.RequestAborted);
                            continue;
                        }

                        if (frame == null ||
                            !string.Equals(frame.Action, "updateVariables", StringComparison.OrdinalIgnoreCase) ||
                            frame.Variables == null)
                        {
                            await SendErrorFrameAsync(socket, "VALIDATION_ERROR", "Action must be updateVariables with full variables payload.", context.RequestAborted);
                            continue;
                        }

                        var score = Math.Round(
                            (frame.Variables.Density * frame.Variables.Volume) / Math.Max(1d, Math.Abs(frame.Variables.Temp) + 1d),
                            4);

                        var payload = JsonSerializer.Serialize(new
                        {
                            canvasState = new
                            {
                                variables = frame.Variables,
                                score,
                                updatedAt = DateTime.UtcNow
                            },
                            feedback = BuildFeedback(frame.Variables)
                        });

                        await SendTextAsync(socket, payload, context.RequestAborted);
                        store.SetLastState(sessionId, payload);

                        await using var scope = context.RequestServices.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<BlueSandsLMSDbContext>();
                        var current = await db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == sessionId, context.RequestAborted);
                        if (current != null)
                        {
                            current.LastSimulationStateJson = payload;
                            current.UpdatedAt = DateTime.UtcNow;
                            await db.SaveChangesAsync(context.RequestAborted);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "INACTIVITY_TIMEOUT",
                            CancellationToken.None);
                    }
                }
                finally
                {
                    store.Remove(sessionId, socket);
                    logger.LogInformation("WS disconnected: sessionId={SessionId}, userId={UserId}", sessionId, userId);
                }
            });
        }

        private static bool TryReadSessionId(HttpContext context, out Guid sessionId)
        {
            sessionId = Guid.Empty;
            var raw = context.Request.RouteValues["sessionId"]?.ToString();
            return Guid.TryParse(raw, out sessionId);
        }

        private static ClaimsPrincipal? ValidateAccessToken(HttpContext context, out string? error)
        {
            error = null;
            var header = context.Request.Headers.Authorization.ToString();
            var token = !string.IsNullOrWhiteSpace(header) && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..].Trim()
                : context.Request.Query["access_token"].ToString();

            if (string.IsNullOrWhiteSpace(token))
            {
                error = "Session invalid or expired";
                return null;
            }

            var cfg = context.RequestServices.GetRequiredService<IConfiguration>();
            var secret = cfg["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                error = "Server auth configuration missing";
                return null;
            }

            var handler = new JwtSecurityTokenHandler();
            try
            {
                return handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = cfg["Jwt:Issuer"],
                    ValidAudience = cfg["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                }, out _);
            }
            catch
            {
                error = "Session invalid or expired";
                return null;
            }
        }

        private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken ct)
        {
            var buffer = new byte[8192];
            using var ms = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                ms.Write(buffer, 0, result.Count);
                if (result.EndOfMessage) break;
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static async Task SendTextAsync(WebSocket socket, string payload, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(payload);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
        }

        private static async Task SendErrorFrameAsync(WebSocket socket, string code, string message, CancellationToken ct)
        {
            var payload = JsonSerializer.Serialize(new { error = true, code, message });
            await SendTextAsync(socket, payload, ct);
        }

        private static async Task RejectSocketAsync(
            HttpContext context,
            string code,
            string message,
            ILogger logger,
            int closeCode = 4001)
        {
            try
            {
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await SendErrorFrameAsync(socket, code, message, context.RequestAborted);
                await socket.CloseAsync((WebSocketCloseStatus)closeCode, code, context.RequestAborted);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "WebSocket rejection failed");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
        }

        private static string BuildFeedback(SimulationVariables variables)
        {
            if (variables.Temp >= 30)
                return "Increasing temperature causes molecules to move faster.";
            if (variables.Density >= 1.5)
                return "Higher density increases downward force in this setup.";
            if (variables.Volume >= 300)
                return "Larger volume amplifies the overall interaction response.";
            return "Variables updated successfully.";
        }
    }
}
