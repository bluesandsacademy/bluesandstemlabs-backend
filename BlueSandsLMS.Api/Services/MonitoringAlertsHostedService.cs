using BlueSandsLMS.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace BlueSandsLMS.Api.Services
{
    public sealed class MonitoringAlertsHostedService : BackgroundService
    {
        private readonly RequestMetricsStore _metricsStore;
        private readonly IOptionsMonitor<MonitoringOptions> _options;
        private readonly ILogger<MonitoringAlertsHostedService> _logger;

        public MonitoringAlertsHostedService(
            RequestMetricsStore metricsStore,
            IOptionsMonitor<MonitoringOptions> options,
            ILogger<MonitoringAlertsHostedService> logger)
        {
            _metricsStore = metricsStore;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var options = _options.CurrentValue;
                var interval = Math.Max(15, options.EvaluateIntervalSeconds);

                if (options.EnableAlerts)
                {
                    EvaluateAlerts(options);
                }

                await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
            }
        }

        private void EvaluateAlerts(MonitoringOptions options)
        {
            var windowMinutes = Math.Max(1, options.WindowMinutes);
            var snapshot = _metricsStore.SnapshotAndPrune(TimeSpan.FromMinutes(windowMinutes));
            if (snapshot.Count == 0)
                return;

            var total = snapshot.Count;
            var serverErrors = snapshot.Count(x => x.StatusCode >= 500);
            var errorRate = total == 0 ? 0 : (serverErrors * 100d) / total;

            if (errorRate > options.ErrorRateThresholdPercent)
            {
                _logger.LogError(
                    "ALERT: 5xx rate exceeded threshold. window={WindowMinutes}m threshold={Threshold}% actual={Actual:F2}% total={Total} 5xx={ServerErrors}",
                    windowMinutes,
                    options.ErrorRateThresholdPercent,
                    errorRate,
                    total,
                    serverErrors);
            }

            var latencyThreshold = options.LatencyP95ThresholdMs;
            foreach (var group in snapshot.GroupBy(x => x.Endpoint))
            {
                if (group.Count() < 10)
                    continue;

                var p95 = ComputeP95(group.Select(x => (double)x.LatencyMs).ToArray());
                if (p95 > latencyThreshold)
                {
                    _logger.LogWarning(
                        "ALERT: endpoint latency P95 exceeded threshold. endpoint={Endpoint} thresholdMs={Threshold} p95Ms={P95:F2} sampleCount={Count}",
                        group.Key,
                        latencyThreshold,
                        p95,
                        group.Count());
                }
            }
        }

        private static double ComputeP95(double[] values)
        {
            if (values.Length == 0)
                return 0;

            Array.Sort(values);
            var index = (int)Math.Ceiling(values.Length * 0.95) - 1;
            if (index < 0) index = 0;
            if (index >= values.Length) index = values.Length - 1;
            return values[index];
        }
    }
}
