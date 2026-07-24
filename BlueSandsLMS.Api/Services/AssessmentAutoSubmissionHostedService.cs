using System.Text.Json;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Services
{

    public sealed class AssessmentAutoSubmissionHostedService : BackgroundService
    {
        private static readonly JsonSerializerOptions AssessmentJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AssessmentAutoSubmissionHostedService> _logger;

        public AssessmentAutoSubmissionHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<AssessmentAutoSubmissionHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processed = await AutoSubmitExpiredAssessmentsAsync(stoppingToken);
                    if (processed > 0)
                        _logger.LogInformation("Auto-submitted {Count} expired assessments.", processed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed while auto-submitting expired assessments.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task<int> AutoSubmitExpiredAssessmentsAsync(CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BlueSandsLMSDbContext>();
            var now = DateTime.UtcNow;

            var candidates = await db.StudentIlsSessions
                .Include(x => x.Ils)
                .Where(x => x.CompletedAt == null && x.CurrentStep >= 5 && x.CurrentStep <= 6)
                .ToListAsync(ct);

            var expired = candidates
                .Where(x => x.Ils != null && x.Ils.DurationMinutes > 0 && x.CreatedAt.AddMinutes(x.Ils.DurationMinutes) <= now)
                .ToList();

            var processed = 0;
            foreach (var session in expired)
            {
                var alreadySubmitted = await db.SessionAssessments
                    .AsNoTracking()
                    .AnyAsync(a => a.SessionId == session.Id, ct);
                if (alreadySubmitted)
                    continue;

                var config = ParseAssessmentConfig(session.Ils?.AssessmentConfigJson);
                var scored = ScoreAssessment(config);
                var submittedAt = DateTime.UtcNow;

                db.SessionAssessments.Add(new SessionAssessment
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id,
                    AnswersJson = "[]",
                    Score = scored.Score,
                    Feedback = $"{scored.Feedback} Auto-submitted because time limit was reached.",
                    IsAutoSubmitted = true,
                    SubmittedAt = submittedAt
                });

                db.AuditEvents.Add(new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    Utc = submittedAt,
                    ActorUserId = session.StudentId,
                    Category = "assessment",
                    Name = "Session.AssessmentScored",
                    ExtraJson = JsonSerializer.Serialize(new
                    {
                        sessionId = session.Id,
                        score = scored.Score,
                        autoSubmitted = true,
                        timestamp = submittedAt
                    })
                });

                session.CurrentStep = 7;
                session.CompletedAt = submittedAt;
                session.UpdatedAt = submittedAt;

                var hasHypothesis = await db.SessionHypotheses.AsNoTracking().AnyAsync(x => x.SessionId == session.Id, ct);
                var hasObservedTrial = await db.SessionTrials.AsNoTracking()
                    .AnyAsync(x => x.SessionId == session.Id && !string.IsNullOrWhiteSpace(x.ObservationText), ct);
                var hasReflection = session.ReflectionSubmitted ||
                                    await db.SessionReflections.AsNoTracking().AnyAsync(x => x.SessionId == session.Id, ct);

                session.BadgeAwarded = hasHypothesis && hasObservedTrial && hasReflection;
                processed++;
            }

            if (processed == 0)
                return 0;

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {

                _logger.LogWarning(ex, "Auto-submit race encountered while saving timed-out assessments.");
            }

            return processed;
        }

        private static AssessmentConfig? ParseAssessmentConfig(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<AssessmentConfig>(json, AssessmentJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static AssessmentScoreResult ScoreAssessment(AssessmentConfig? config)
        {
            if (config == null || config.Questions.Count == 0)
            {
                return new AssessmentScoreResult(
                    0m,
                    "Assessment auto-submitted with score 0.0 because no valid assessment configuration was found.");
            }

            decimal possible = 0m;
            foreach (var q in config.Questions.Where(q => !string.IsNullOrWhiteSpace(q.QId)))
            {
                possible += q.Points <= 0 ? 1m : q.Points;
            }

            var score = possible <= 0 ? 0m : 0m;
            return new AssessmentScoreResult(
                score,
                "Assessment auto-submitted with score 0.0 due to timeout.");
        }

        private sealed class AssessmentConfig
        {
            public List<AssessmentQuestion> Questions { get; set; } = new();
        }

        private sealed class AssessmentQuestion
        {
            public string QId { get; set; } = string.Empty;
            public decimal Points { get; set; } = 1m;
        }

        private readonly record struct AssessmentScoreResult(decimal Score, string Feedback);
    }
}
