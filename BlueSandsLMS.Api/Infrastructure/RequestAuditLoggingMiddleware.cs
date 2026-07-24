using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace BlueSandsLMS.Api.Infrastructure
{
    public sealed class RequestAuditLoggingMiddleware
    {
        private static readonly Regex PiiRegex = new(
            "\"(password|email|token|secret|authorization)\"\\s*:\\s*\".*?\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestAuditLoggingMiddleware> _logger;
        private readonly RequestMetricsStore _metricsStore;

        public RequestAuditLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestAuditLoggingMiddleware> logger,
            RequestMetricsStore metricsStore)
        {
            _next = next;
            _logger = logger;
            _metricsStore = metricsStore;
        }

        public async Task Invoke(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            string? requestBody = null;

            if (CanCaptureRequestBody(context.Request))
            {
                requestBody = await ReadBodySafeAsync(context.Request);
            }

            await _next(context);

            stopwatch.Stop();
            var status = context.Response.StatusCode;
            var endpointPath = context.Request.Path.Value ?? string.Empty;
            _metricsStore.Record(endpointPath, status, stopwatch.ElapsedMilliseconds);
            var anonymizedUserId = HashUserId(context.User);

            _logger.LogInformation(
                "request method={Method} endpoint={Endpoint} status={Status} latencyMs={LatencyMs} userId={UserId}",
                context.Request.Method,
                endpointPath,
                status,
                stopwatch.ElapsedMilliseconds,
                anonymizedUserId);

            if (status < 400)
                return;

            var code = ApiErrorFactory.TryReadCode(context) ?? ApiErrorFactory.CodeForStatus(status);
            var message = ApiErrorFactory.TryReadMessage(context) ?? ReasonPhrases.GetReasonPhrase(status);

            _logger.LogWarning(
                "error method={Method} endpoint={Endpoint} status={Status} code={Code} message={Message} requestBody={RequestBody}",
                context.Request.Method,
                endpointPath,
                status,
                code,
                message,
                SanitizeBodyForLogs(requestBody));
        }

        private static bool CanCaptureRequestBody(HttpRequest request)
        {
            if (request.ContentLength is null or <= 0)
                return false;

            if (request.ContentLength > 32 * 1024)
                return false;

            return HttpMethods.IsPost(request.Method) ||
                   HttpMethods.IsPut(request.Method) ||
                   HttpMethods.IsPatch(request.Method);
        }

        private static async Task<string?> ReadBodySafeAsync(HttpRequest request)
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }

        private static string HashUserId(ClaimsPrincipal user)
        {
            var rawUserId = user.FindFirstValue("sub") ??
                            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                            "anonymous";

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawUserId));
            return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
        }

        private static string SanitizeBodyForLogs(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            var compact = body.Length > 2048 ? body[..2048] + "...[truncated]" : body;
            return PiiRegex.Replace(compact, "\"$1\":\"***\"");
        }
    }
}
