using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Infrastructure
{

    public sealed class ApiErrorResponse
    {

        public bool Error { get; set; } = true;

        public string Code { get; set; } = "SERVER_ERROR";

        public string Message { get; set; } = "An unexpected server error occurred.";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ApiErrorDetail>? Details { get; set; }
    }

    public sealed class ApiErrorDetail
    {

        public string Field { get; set; } = string.Empty;

        public string Issue { get; set; } = string.Empty;
    }

    public static class ApiErrorFactory
    {
        public const string ErrorCodeItemKey = "__api_error_code";
        public const string ErrorMessageItemKey = "__api_error_message";

        public static ApiErrorResponse Create(
            int statusCode,
            string? code = null,
            string? message = null,
            IEnumerable<ApiErrorDetail>? details = null)
        {
            var payload = new ApiErrorResponse
            {
                Code = code ?? CodeForStatus(statusCode),
                Message = message ?? DefaultMessageForStatus(statusCode)
            };

            if (details != null)
            {
                var list = details
                    .Where(d => !string.IsNullOrWhiteSpace(d.Field) || !string.IsNullOrWhiteSpace(d.Issue))
                    .ToList();
                if (list.Count > 0)
                    payload.Details = list;
            }

            return payload;
        }

        public static string CodeForStatus(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => "VALIDATION_ERROR",
            StatusCodes.Status401Unauthorized => "AUTH_REQUIRED",
            StatusCodes.Status403Forbidden => "FORBIDDEN",
            StatusCodes.Status404NotFound => "NOT_FOUND",
            StatusCodes.Status409Conflict => "CONFLICT",
            StatusCodes.Status422UnprocessableEntity => "BUSINESS_RULE_ERROR",
            StatusCodes.Status429TooManyRequests => "RATE_LIMITED",
            _ => "SERVER_ERROR"
        };

        public static string DefaultMessageForStatus(int statusCode) => statusCode switch
        {
            StatusCodes.Status400BadRequest => "Validation failed.",
            StatusCodes.Status401Unauthorized => "Authentication required.",
            StatusCodes.Status403Forbidden => "You are not allowed to perform this action.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status409Conflict => "The request conflicts with existing state.",
            StatusCodes.Status422UnprocessableEntity => "The request violates a business rule.",
            StatusCodes.Status429TooManyRequests => "Too many requests. Please retry later.",
            _ => "An unexpected server error occurred."
        };

        public static void Stamp(HttpContext context, string code, string message)
        {
            context.Items[ErrorCodeItemKey] = code;
            context.Items[ErrorMessageItemKey] = message;
        }

        public static string? TryReadCode(HttpContext context) =>
            context.Items.TryGetValue(ErrorCodeItemKey, out var value) ? value?.ToString() : null;

        public static string? TryReadMessage(HttpContext context) =>
            context.Items.TryGetValue(ErrorMessageItemKey, out var value) ? value?.ToString() : null;
    }
}
