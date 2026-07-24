namespace BlueSandsLMS.Api.Infrastructure
{
    public sealed class MonitoringOptions
    {
        public bool EnableAlerts { get; set; } = true;
        public int WindowMinutes { get; set; } = 5;
        public int EvaluateIntervalSeconds { get; set; } = 60;
        public double LatencyP95ThresholdMs { get; set; } = 500;
        public double ErrorRateThresholdPercent { get; set; } = 1;
        public UptimeOptions Uptime { get; set; } = new();
    }

    public sealed class UptimeOptions
    {
        public bool Enabled { get; set; } = true;
        public string? BaseUrl { get; set; }
        public int IntervalSeconds { get; set; } = 30;
        public List<string> Endpoints { get; set; } = new()
        {
            "/health/live",
            "/health/ready"
        };
    }
}
