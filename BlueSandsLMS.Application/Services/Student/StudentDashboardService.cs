using System.Linq;
using BlueSandsLMS.Common.DTOs.Student;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BlueSandsLMS.Application.Services.Student
{
    public sealed class StudentDashboardService : IStudentDashboardService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IMemoryCache _cache;

        public StudentDashboardService(BlueSandsLMSDbContext db, IMemoryCache cache)
        { _db = db; _cache = cache; }

        public async Task<StudentOverviewDto> GetOverviewAsync(Guid userId, CancellationToken ct = default)
        {
            var key = $"st:ov:{userId}";
            if (_cache.TryGetValue(key, out StudentOverviewDto cached)) return cached;

            var since7 = DateTime.UtcNow.AddDays(-7);

            var exps = await _db.ExperimentLaunches
                .Where(x => x.UserId == userId)
                .ToListAsync(ct);

            var completed = exps.Count(x => x.EndedAt != null);
            var inprog = exps.Count(x => x.EndedAt == null);
            var minutes = exps.Where(x => x.StartedAt >= since7).Sum(x =>
            {
                var end = x.EndedAt ?? DateTime.UtcNow;
                return (int)Math.Max(0, (end - x.StartedAt).TotalMinutes);
            });

            var attempts = await _db.QuizAttempts.Where(x => x.UserId == userId).ToListAsync(ct);
            var avg = attempts.Count == 0 ? 0.0 : Math.Round(attempts.Average(a => (double)(a.Score0to1 * 100m)), 1);

            var badges = await _db.BadgeAwards.CountAsync(x => x.UserId == userId, ct);

            // TODO: compute real ranks (class/school) later
            var classRank = 1;
            var schoolRank = 1;

            var dto = new StudentOverviewDto(
                CompletedExperiments: completed,
                InProgressExperiments: inprog,
                AvgQuizScorePercent: avg,
                BadgesCount: badges,
                MinutesInLab7d: minutes,
                RankClass: classRank,
                RankSchool: schoolRank,
                Greeting: "Welcome back 👋",
                Recommendations: new[] { "Complete your pending experiment", "Try a post-assessment quiz" }
            );

            _cache.Set(key, dto, TimeSpan.FromMinutes(2));
            return dto;
        }

        public async Task<IReadOnlyList<StudentAttemptDto>> GetRecentQuizAttemptsAsync(Guid userId, int take = 20, CancellationToken ct = default)
        {
            take = Math.Clamp(take <= 0 ? 20 : take, 1, 100);

            return await _db.QuizAttempts
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CompletedAt)
                .Take(take)
                .Select(x => new StudentAttemptDto(
                    x.Id, x.Subject, x.QuizCode,
                    (double)(x.Score0to1 * 100m), x.Passed, x.CompletedAt ?? DateTime.MinValue))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<StudentExperimentDto>> GetRecentExperimentsAsync(Guid userId, int take = 20, CancellationToken ct = default)
        {
            take = Math.Clamp(take <= 0 ? 20 : take, 1, 100);

            return await _db.ExperimentLaunches
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.StartedAt)
                .Take(take)
                .Select(x => new StudentExperimentDto(
                    x.Id, x.Subject, x.ExperimentName, x.Mode, x.LastStep, x.StartedAt, x.EndedAt))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<StudentBadgeDto>> GetBadgesAsync(Guid userId, CancellationToken ct = default)
        {
            return await _db.BadgeAwards
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.DateCreated)
                .Select(x => new StudentBadgeDto(x.Code, x.Name, x.Description, x.DateCreated))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<StudentLeaderboardEntry>> GetLeaderboardAsync(Guid userId, string scope, int take = 10, CancellationToken ct = default)
        {
            take = Math.Clamp(take <= 0 ? 10 : take, 1, 50);

            var top = await _db.QuizAttempts
                .GroupBy(x => x.UserId)
                .Select(g => new { g.Key, Avg = g.Average(x => x.Score0to1) })
                .OrderByDescending(x => x.Avg)
                .Take(take)
                .ToListAsync(ct);

            var ids = top.Select(x => x.Key).ToList();
            var names = await _db.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync(ct);

            return top
                .Select(t => new StudentLeaderboardEntry(
                    t.Key,
                    names.FirstOrDefault(n => n.Id == t.Key)?.FullName ?? "(Student)",
                    (double)(t.Avg * 100m)))
                .ToList();
        }
    }
}
