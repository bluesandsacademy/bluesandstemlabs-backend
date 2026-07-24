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


            var completed = await _db.ExperimentLaunches.AsNoTracking()
                .CountAsync(x => x.UserId == userId && x.Completed, ct);

            var inprog = await _db.ExperimentLaunches.AsNoTracking()
                .CountAsync(x => x.UserId == userId && !x.Completed, ct);

            var minutesSec = await _db.ExperimentLaunches.AsNoTracking()
                .Where(x => x.UserId == userId && x.StartedAt >= since7)
                .SumAsync(x => (int?)x.DurationSec, ct) ?? 0;
            var minutes = minutesSec / 60;


            var quizStats = await _db.QuizAttempts.AsNoTracking()
                .Where(x => x.UserId == userId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Attempted = g.Count(),
                    Passed    = g.Count(x => x.Passed),
                    AvgScore  = g.Average(x => (decimal?)x.Score0to1),
                    Recent    = g.Max(x => x.CompletedAt)
                })
                .FirstOrDefaultAsync(ct);

            var quizzesAttempted = quizStats?.Attempted ?? 0;
            var quizzesPassed    = quizStats?.Passed ?? 0;
            var avg = Math.Round((double)((quizStats?.AvgScore ?? 0m) * 100m), 1);
            var mostRecentAttemptDate = quizStats?.Recent;

            var badges = await _db.BadgeAwards.AsNoTracking()
                .CountAsync(x => x.UserId == userId, ct);


            var myAvg = (quizStats?.AvgScore ?? 0m);

            var myClassIds = await _db.Enrollments.AsNoTracking()
                .Where(e => e.UserId == userId && e.RoleInClass == Core.Entities.ClassRole.Student)
                .Select(e => e.ClassroomId)
                .Distinct()
                .ToListAsync(ct);

            int classRank = 1;
            if (myClassIds.Count > 0)
            {
                var peerIds = await _db.Enrollments.AsNoTracking()
                    .Where(e => myClassIds.Contains(e.ClassroomId)
                             && e.RoleInClass == Core.Entities.ClassRole.Student
                             && e.UserId != userId)
                    .Select(e => e.UserId)
                    .Distinct()
                    .ToListAsync(ct);

                var betterInClass = await _db.QuizAttempts.AsNoTracking()
                    .Where(q => peerIds.Contains(q.UserId))
                    .GroupBy(q => q.UserId)
                    .Where(g => g.Average(x => x.Score0to1) > myAvg)
                    .CountAsync(ct);

                classRank = betterInClass + 1;
            }

            int schoolRank = 1;
            var userSchoolId = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(ct);

            if (userSchoolId.HasValue)
            {
                var schoolStudentIds = await _db.Users.AsNoTracking()
                    .Where(u => u.SchoolId == userSchoolId.Value
                             && u.IsActive
                             && u.Role!.Name == "Student"
                             && u.Id != userId)
                    .Select(u => u.Id)
                    .ToListAsync(ct);

                var betterInSchool = await _db.QuizAttempts.AsNoTracking()
                    .Where(q => schoolStudentIds.Contains(q.UserId))
                    .GroupBy(q => q.UserId)
                    .Where(g => g.Average(x => x.Score0to1) > myAvg)
                    .CountAsync(ct);

                schoolRank = betterInSchool + 1;
            }

            var dto = new StudentOverviewDto(
                CompletedExperiments: completed,
                InProgressExperiments: inprog,
                AvgQuizScorePercent: avg,
                BadgesCount: badges,
                MinutesInLab7d: minutes,
                RankClass: classRank,
                RankSchool: schoolRank,
                Greeting: "Welcome back",
                Recommendations: new[] { "Complete your pending experiment", "Try a post-assessment quiz" }
            )
            {
                QuizzesAttempted      = quizzesAttempted,
                QuizzesPassed         = quizzesPassed,
                MostRecentAttemptDate = mostRecentAttemptDate
            };

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
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.StartedAt)
                .Take(take)
                .Select(x => new StudentExperimentDto(
                    x.Id, x.Subject, x.ExperimentName, x.Mode, x.LastStep, x.StartedAt, x.EndedAt)
                {
                    Completed       = x.Completed,
                    DurationMinutes = x.DurationSec / 60
                })
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
