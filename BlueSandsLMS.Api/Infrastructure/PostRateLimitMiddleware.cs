namespace BlueSandsLMS.Api.Infrastructure
{
    public sealed class PostRateLimitMiddleware
    {
        private readonly RequestDelegate _next;

        public PostRateLimitMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context, PerIpRateLimitService limiter)
        {
            if (!HttpMethods.IsPost(context.Request.Method))
            {
                await _next(context);
                return;
            }

            if (limiter.TryConsumePost(context, out var retryAfter, out _))
            {
                await _next(context);
                return;
            }

            var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

            var payload = ApiErrorFactory.Create(
                StatusCodes.Status429TooManyRequests,
                message: $"Rate limit exceeded. Retry after {retryAfterSeconds} seconds.");

            ApiErrorFactory.Stamp(context, payload.Code, payload.Message);
            await context.Response.WriteAsJsonAsync(payload);
        }
    }
}
