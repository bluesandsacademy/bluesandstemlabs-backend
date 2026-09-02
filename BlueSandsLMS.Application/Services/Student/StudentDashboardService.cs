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
        {
            _db = db;
            _cache = cache;
        }

        public async Task<StudentOverviewDto> GetOverviewAsync(Guid userId, CancellationToken ct = default)
        {
            var key = $"st:ov:{userId}";
            if (_cache.TryGetValue(key, out StudentOverviewDto cached)) return cached;

            // Hardcoded mock values reflecting an active student profile
            var dto = new StudentOverviewDto(
                CompletedExperiments: 12,
                InProgressExperiments: 2,
                AvgQuizScorePercent: 84.5,
                BadgesCount: 5,
                MinutesInLab7d: "450 minutes",
                RankClass: 3,
                RankSchool: 14,
                Greeting: "Welcome back",
                Recommendations: new[] { "Complete your pending experiment", "Try a post-assessment quiz" }
            )
            {
                QuizzesAttempted = 15,
                QuizzesPassed = 13,
                MostRecentAttemptDate = DateTime.UtcNow.AddHours(-4)
            };

            _cache.Set(key, dto, TimeSpan.FromMinutes(2));
            return await Task.FromResult(dto);
        }

        public async Task<IReadOnlyList<StudentAttemptDto>> GetRecentQuizAttemptsAsync(Guid userId, int take = 20, CancellationToken ct = default)
        {
            var attempts = new List<StudentAttemptDto>
            {
                new(Guid.NewGuid(), "Chemistry", "CHEM-101", 90.0, true, DateTime.UtcNow.AddHours(-4)),
                new(Guid.NewGuid(), "Physics", "PHYS-201", 85.0, true, DateTime.UtcNow.AddDays(-1)),
                new(Guid.NewGuid(), "Biology", "BIO-105", 78.5, true, DateTime.UtcNow.AddDays(-3)),
                new(Guid.NewGuid(), "Mathematics", "MATH-110", 45.0, false, DateTime.UtcNow.AddDays(-5)),
                new(Guid.NewGuid(), "Chemistry", "CHEM-102", 92.0, true, DateTime.UtcNow.AddDays(-7))
            };

            return await Task.FromResult(attempts.Take(take).ToList());
        }

        public async Task<IReadOnlyList<StudentExperimentDto>> GetRecentExperimentsAsync(Guid userId, int take = 20, CancellationToken ct = default)
        {
            var experiments = new List<StudentExperimentDto>
    {
        // Passed '4' instead of "Step 4 - Observation"
        new(Guid.NewGuid(), "Chemistry", "Acid-Base Titration", "Lab", 4, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddMinutes(45))
        {
            Completed = true,
            DurationMinutes = 45
        },
        // Passed '2' instead of "Step 2 - Circuit Setup"
        new(Guid.NewGuid(), "Physics", "Ohm's Law Verification", "Lab", 2, DateTime.UtcNow.AddDays(-2), null)
        {
            Completed = false,
            DurationMinutes = 20
        },
        // Passed '5' instead of "Completed"
        new(Guid.NewGuid(), "Biology", "Cell Division Analysis", "Simulation", 5, DateTime.UtcNow.AddDays(-4), DateTime.UtcNow.AddDays(-4).AddMinutes(35))
        {
            Completed = true,
            DurationMinutes = 35
        }
    };

            return await Task.FromResult(experiments.Take(take).ToList());
        }

        public async Task<IReadOnlyList<StudentBadgeDto>> GetBadgesAsync(Guid userId, CancellationToken ct = default)
        {
            var badges = new List<StudentBadgeDto>
            {
                new("LAB_MASTER", "Lab Master", "Completed 10 virtual lab experiments.", DateTime.UtcNow.AddDays(-10)),
                new("QUIZ_WHIZ", "Quiz Whiz", "Scored over 90% on three consecutive quizzes.", DateTime.UtcNow.AddDays(-15)),
                new("EARLY_BIRD", "Early Bird", "Completed an assignment 48 hours before the deadline.", DateTime.UtcNow.AddDays(-20)),
                new("STREAK_7", "7-Day Streak", "Logged into the LMS for 7 consecutive days.", DateTime.UtcNow.AddDays(-25)),
                new("TOP_SCORER", "Top Scorer", "Ranked in the top 5 of your class.", DateTime.UtcNow.AddDays(-30))
            };

            return await Task.FromResult(badges);
        }

        public async Task<IReadOnlyList<StudentLeaderboardEntry>> GetLeaderboardAsync(Guid userId, string scope, int take = 10, CancellationToken ct = default)
        {
            var leaderboard = new List<StudentLeaderboardEntry>
            {
                new(Guid.NewGuid(), "Alex Johnson", 96.8),
                new(Guid.NewGuid(), "Sarah Williams", 94.2),
                new(userId, "You (Current Student)", 84.5),
                new(Guid.NewGuid(), "David Miller", 82.1),
                new(Guid.NewGuid(), "Emily Davis", 79.4)
            };

            return await Task.FromResult(leaderboard.Take(take).ToList());
        }
    }
}