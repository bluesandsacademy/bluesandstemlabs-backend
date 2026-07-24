using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Services
{

    public sealed class TrialExpiryHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrialExpiryHostedService> _logger;

        public TrialExpiryHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<TrialExpiryHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var expired = await ExpireTrialsAsync(stoppingToken);
                    if (expired > 0)
                        _logger.LogInformation(
                            "TrialExpiry: deactivated {Count} expired trial subscription(s).", expired);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TrialExpiry: error while expiring trials.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task<int> ExpireTrialsAsync(CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db  = scope.ServiceProvider.GetRequiredService<BlueSandsLMSDbContext>();
            var now = DateTime.UtcNow;

            var expiredTrials = await db.Subscriptions
                .Where(s => s.Active &&
                            s.EndsAt < now &&
                            s.LastPaymentReference == "TRIAL")
                .ToListAsync(ct);

            foreach (var trial in expiredTrials)
                trial.Active = false;

            if (expiredTrials.Count > 0)
                await db.SaveChangesAsync(ct);

            return expiredTrials.Count;
        }
    }
}
