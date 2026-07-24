using BlueSandsLMS.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace BlueSandsLMS.Api.Services
{
    public sealed class CriticalEndpointUptimeHostedService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<MonitoringOptions> _options;
        private readonly IServer _server;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CriticalEndpointUptimeHostedService> _logger;

        public CriticalEndpointUptimeHostedService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<MonitoringOptions> options,
            IServer server,
            IWebHostEnvironment environment,
            ILogger<CriticalEndpointUptimeHostedService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _server = server;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var options = _options.CurrentValue;
                var intervalSeconds = Math.Max(15, options.Uptime.IntervalSeconds);

                if (options.Uptime.Enabled)
                {
                    await ProbeEndpointsAsync(options, stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
        }

        private async Task ProbeEndpointsAsync(MonitoringOptions options, CancellationToken ct)
        {
            if (!TryResolveBaseUri(options, out var baseUri))
            {
                return;
            }

            var endpoints = options.Uptime.Endpoints ?? new List<string>();
            if (endpoints.Count == 0)
                return;

            var client = _httpClientFactory.CreateClient("monitoring-uptime");
            client.Timeout = TimeSpan.FromSeconds(8);

            foreach (var endpoint in endpoints.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                var uri = new Uri(baseUri, endpoint);
                try
                {
                    using var response = await client.GetAsync(uri, ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError(
                            "UPTIME ALERT: endpoint check failed. endpoint={Endpoint} status={StatusCode}",
                            uri.ToString(),
                            (int)response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UPTIME ALERT: endpoint check failed. endpoint={Endpoint}", uri.ToString());
                }
            }
        }

        private bool TryResolveBaseUri(MonitoringOptions options, out Uri baseUri)
        {
            baseUri = default!;

            Uri? configuredUri = null;
            if (!string.IsNullOrWhiteSpace(options.Uptime.BaseUrl) &&
                Uri.TryCreate(options.Uptime.BaseUrl, UriKind.Absolute, out var parsedConfiguredUri))
            {
                configuredUri = parsedConfiguredUri;
            }

            var hasRuntimeAddress = TryGetRuntimeBaseUri(out var runtimeUri);

            if (_environment.IsDevelopment())
            {
                if (hasRuntimeAddress)
                {
                    baseUri = runtimeUri;
                    return true;
                }

                return false;
            }

            if (configuredUri is not null)
            {
                if (hasRuntimeAddress &&
                    IsLoopback(configuredUri.Host) &&
                    IsLoopback(runtimeUri!.Host) &&
                    configuredUri.Port != runtimeUri.Port)
                {
                    _logger.LogInformation(
                        "Monitoring uptime base URL port mismatch detected (configured={ConfiguredPort}, runtime={RuntimePort}); using runtime URL {RuntimeUrl}.",
                        configuredUri.Port,
                        runtimeUri.Port,
                        runtimeUri.ToString());
                    baseUri = runtimeUri;
                    return true;
                }

                baseUri = configuredUri;
                return true;
            }

            if (hasRuntimeAddress)
            {
                baseUri = runtimeUri;
                return true;
            }

            _logger.LogWarning("Uptime probe skipped: unable to resolve a valid base URL.");
            return false;
        }

        private bool TryGetRuntimeBaseUri(out Uri runtimeUri)
        {
            runtimeUri = default!;

            var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
            if (addresses is null || addresses.Count == 0)
                return false;

            foreach (var address in addresses)
            {
                if (!Uri.TryCreate(address, UriKind.Absolute, out var parsed))
                    continue;

                if (parsed.Host == "0.0.0.0" || parsed.Host == "::" || parsed.Host == "[::]")
                {
                    var builder = new UriBuilder(parsed) { Host = "localhost" };
                    parsed = builder.Uri;
                }

                runtimeUri = parsed;
                return true;
            }

            return false;
        }

        private static bool IsLoopback(string host) =>
            host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("[::1]", StringComparison.OrdinalIgnoreCase);
    }
}
