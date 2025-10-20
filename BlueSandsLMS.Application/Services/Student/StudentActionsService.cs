using BlueSandsLMS.Common.DTOs.Student;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services.Student
{
    public sealed class StudentActionsService : IStudentActionsService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IBadgeEngine _badges;

        public StudentActionsService(BlueSandsLMSDbContext db, IBadgeEngine badges)
        {
            _db = db; _badges = badges;
        }

        public async Task<StartExperimentResponse> StartExperimentAsync(Guid userId, StartExperimentRequest req, CancellationToken ct)
        {
            var existing = await _db.ExperimentLaunches
                .Where(x => x.UserId == userId && x.ExperimentName == req.ExperimentName && x.EndedAt == null)
                .OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(ct);

            if (existing is not null) return new(existing.Id);

            var launch = new ExperimentLaunch
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ClassroomId = req.ClassroomId ?? Guid.Empty,
                Subject = req.Subject,
                ExperimentName = req.ExperimentName,
                Mode = string.IsNullOrWhiteSpace(req.Mode) ? "guided" : req.Mode,
                LastStep = 1,
                StartedAt = DateTime.UtcNow
            };
            _db.ExperimentLaunches.Add(launch);
            await _db.SaveChangesAsync(ct);

            await _badges.AwardAsync(userId, "FIRST_LAUNCH", new { req.ExperimentName }, ct);
            return new StartExperimentResponse(launch.Id);
        }

        public async Task SaveExperimentProgressAsync(Guid userId, Guid launchId, SaveExperimentProgressRequest req, CancellationToken ct)
        {
            var launch = await _db.ExperimentLaunches.FirstOrDefaultAsync(x => x.Id == launchId && x.UserId == userId, ct)
                         ?? throw new InvalidOperationException("Launch not found");
            if (req.LastStep > launch.LastStep) launch.LastStep = req.LastStep;
            await _db.SaveChangesAsync(ct);
        }

        public async Task CompleteExperimentAsync(Guid userId, Guid launchId, CompleteExperimentRequest req, CancellationToken ct)
        {
            var launch = await _db.ExperimentLaunches.FirstOrDefaultAsync(x => x.Id == launchId && x.UserId == userId, ct)
                         ?? throw new InvalidOperationException("Launch not found");
            if (launch.EndedAt is null) launch.EndedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _badges.AwardAsync(userId, "COMPLETED_EXPERIMENT", new { launch.ExperimentName }, ct);
        }

        public async Task<SubmitQuizResponse> SubmitQuizAsync(Guid userId, SubmitQuizRequest req, CancellationToken ct)
        {
            decimal score01 = 0m;
            if (req.Questions.Count > 0 && req.Questions.All(q => q.Correct is not null))
            {
                var correct = req.Questions.Count(q => string.Equals(q.Answer, q.Correct, StringComparison.OrdinalIgnoreCase));
                score01 = (decimal)correct / req.Questions.Count;
            }
            else
            {
                throw new InvalidOperationException("Server-side scoring not configured and no 'correct' keys supplied.");
            }

            var attempt = new QuizAttempt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ClassroomId = Guid.Empty,
                Subject = req.Subject,
                QuizCode = req.QuizCode,
                Score0to1 = score01,
                Passed = score01 >= 0.5m,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Type = req.Type,
                ExperimentLaunchId = req.ExperimentLaunchId
            };

            _db.QuizAttempts.Add(attempt);
            await _db.SaveChangesAsync(ct);

            if (attempt.Score0to1 >= 0.9m) await _badges.AwardAsync(userId, "SCORE_90_PLUS", new { req.QuizCode }, ct);

            return new SubmitQuizResponse(attempt.Id, (double)(attempt.Score0to1 * 100m), attempt.Passed);
        }
    }
}
