using System.Collections.Concurrent;

namespace BlueSandsLMS.Api.Infrastructure
{
    public sealed class RequestMetricsStore
    {
        private readonly ConcurrentQueue<RequestMetric> _samples = new();

        public void Record(string endpoint, int statusCode, long latencyMs)
        {
            _samples.Enqueue(new RequestMetric(
                DateTimeOffset.UtcNow,
                endpoint,
                statusCode,
                latencyMs));

            while (_samples.Count > 50_000)
                _samples.TryDequeue(out _);
        }

        public List<RequestMetric> SnapshotAndPrune(TimeSpan window)
        {
            var cutoff = DateTimeOffset.UtcNow.Subtract(window);
            while (_samples.TryPeek(out var oldest) && oldest.At < cutoff)
                _samples.TryDequeue(out _);

            return _samples.ToList();
        }
    }

    public readonly record struct RequestMetric(
        DateTimeOffset At,
        string Endpoint,
        int StatusCode,
        long LatencyMs);
}
