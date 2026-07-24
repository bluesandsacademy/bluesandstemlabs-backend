using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BlueSandsLMS.Api.Infrastructure
{
    public sealed class ApiErrorEnvelopeFilter : IAsyncAlwaysRunResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (TryNormalize(context, out var normalizedResult))
                context.Result = normalizedResult;

            await next();
        }

        private static bool TryNormalize(ResultExecutingContext context, out IActionResult normalizedResult)
        {
            normalizedResult = context.Result;

            if (context.Result is UnauthorizedResult)
            {
                var payload = ApiErrorFactory.Create(StatusCodes.Status401Unauthorized);
                Stamp(context.HttpContext, payload);
                normalizedResult = new ObjectResult(payload) { StatusCode = StatusCodes.Status401Unauthorized };
                return true;
            }

            if (context.Result is ForbidResult)
            {
                var payload = ApiErrorFactory.Create(StatusCodes.Status403Forbidden);
                Stamp(context.HttpContext, payload);
                normalizedResult = new ObjectResult(payload) { StatusCode = StatusCodes.Status403Forbidden };
                return true;
            }

            if (context.Result is StatusCodeResult statusCodeResult && statusCodeResult.StatusCode >= 400)
            {
                var payload = ApiErrorFactory.Create(statusCodeResult.StatusCode);
                Stamp(context.HttpContext, payload);
                normalizedResult = new ObjectResult(payload) { StatusCode = statusCodeResult.StatusCode };
                return true;
            }

            if (context.Result is not ObjectResult objectResult)
                return false;

            var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;
            if (statusCode < 400)
                return false;

            if (LooksLikeErrorEnvelope(objectResult.Value, out var existingCode, out var existingMessage))
            {
                if (!string.IsNullOrWhiteSpace(existingCode) && !string.IsNullOrWhiteSpace(existingMessage))
                {
                    ApiErrorFactory.Stamp(context.HttpContext, existingCode!, existingMessage!);
                    return false;
                }
            }

            var details = ExtractDetails(objectResult.Value);
            var message = ExtractMessage(objectResult.Value) ?? ApiErrorFactory.DefaultMessageForStatus(statusCode);
            var payload2 = ApiErrorFactory.Create(statusCode, message: message, details: details);
            Stamp(context.HttpContext, payload2);
            normalizedResult = new ObjectResult(payload2) { StatusCode = statusCode };
            return true;
        }

        private static void Stamp(HttpContext context, ApiErrorResponse payload) =>
            ApiErrorFactory.Stamp(context, payload.Code, payload.Message);

        private static bool LooksLikeErrorEnvelope(object? value, out string? code, out string? message)
        {
            code = null;
            message = null;
            if (value == null) return false;

            JsonElement element;
            try
            {
                element = JsonSerializer.SerializeToElement(value);
            }
            catch
            {
                return false;
            }

            if (element.ValueKind != JsonValueKind.Object) return false;
            if (!TryGetProperty(element, "error", out var errorProp) ||
                errorProp.ValueKind != JsonValueKind.True)
                return false;

            if (TryGetProperty(element, "code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
                code = codeProp.GetString();
            if (TryGetProperty(element, "message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                message = messageProp.GetString();

            return true;
        }

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
        {
            if (element.TryGetProperty(name, out value))
                return true;

            var pascalName = char.ToUpperInvariant(name[0]) + name[1..];
            return element.TryGetProperty(pascalName, out value);
        }

        private static IEnumerable<ApiErrorDetail>? ExtractDetails(object? value)
        {
            switch (value)
            {
                case ValidationProblemDetails validation:
                    return validation.Errors
                        .SelectMany(kvp => kvp.Value.Select(issue => new ApiErrorDetail
                        {
                            Field = kvp.Key,
                            Issue = issue
                        }))
                        .ToList();

                case SerializableError serializable:
                    var items = new List<ApiErrorDetail>();
                    foreach (var kvp in serializable)
                    {
                        if (kvp.Value is string singleIssue)
                        {
                            items.Add(new ApiErrorDetail { Field = kvp.Key, Issue = singleIssue });
                            continue;
                        }

                        if (kvp.Value is string[] issueList)
                        {
                            items.AddRange(issueList.Select(issue => new ApiErrorDetail
                            {
                                Field = kvp.Key,
                                Issue = issue
                            }));
                        }
                    }

                    return items;
            }

            return null;
        }

        private static string? ExtractMessage(object? value)
        {
            if (value == null)
                return null;

            if (value is string text && !string.IsNullOrWhiteSpace(text))
                return text.Trim();

            if (value is ProblemDetails pd && !string.IsNullOrWhiteSpace(pd.Detail))
                return pd.Detail;

            JsonElement element;
            try
            {
                element = JsonSerializer.SerializeToElement(value);
            }
            catch
            {
                return null;
            }

            if (element.ValueKind != JsonValueKind.Object)
                return null;

            if (element.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                return msgProp.GetString();

            if (element.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                return detailProp.GetString();

            return null;
        }
    }
}
