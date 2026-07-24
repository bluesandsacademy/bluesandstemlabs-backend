using System.Collections.Concurrent;
using System.Security.Claims;

namespace BlueSandsLMS.Api.Infrastructure
{
    public sealed class PerIpRateLimitService
    {
        private readonly ConcurrentDictionary<string, WindowCounter> _windows = new();

        public bool TryConsumePost(HttpContext context, out TimeSpan retryAfter, out int limit)
        {
            var role = context.User.FindFirstValue(ClaimTypes.Role) ??
                       context.User.FindFirstValue("role") ??
                       string.Empty;

            limit = string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) ? 120 : 60;
            var ip = ReadIp(context);
            return TryConsume($"http:{ip}", limit, TimeSpan.FromMinutes(1), out retryAfter);
        }

        public bool TryConsumeWebSocketConnect(HttpContext context, out TimeSpan retryAfter)
        {
            var ip = ReadIp(context);
            return TryConsume($"ws:{ip}", 10, TimeSpan.FromMinutes(1), out retryAfter);
        }

        private bool TryConsume(string key, int limit, TimeSpan window, out TimeSpan retryAfter)
        {
            var now = DateTimeOffset.UtcNow;
            var counter = _windows.GetOrAdd(key, _ => new WindowCounter(now));

            lock (counter.Gate)
            {
                var elapsed = now - counter.WindowStart;
                if (elapsed >= window)
                {
                    counter.WindowStart = now;
                    counter.Count = 0;
                    elapsed = TimeSpan.Zero;
                }

                if (counter.Count >= limit)
                {
                    retryAfter = window - elapsed;
                    if (retryAfter < TimeSpan.Zero)
                        retryAfter = TimeSpan.Zero;
                    return false;
                }

                counter.Count++;
                retryAfter = TimeSpan.Zero;
                return true;
            }
        }

        private static string ReadIp(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private sealed class WindowCounter
        {
            public WindowCounter(DateTimeOffset now) => WindowStart = now;

            public object Gate { get; } = new();
            public DateTimeOffset WindowStart { get; set; }
            public int Count { get; set; }
        }
    }
}
