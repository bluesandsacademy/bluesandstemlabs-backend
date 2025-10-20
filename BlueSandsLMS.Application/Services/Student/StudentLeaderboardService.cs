using BlueSandsLMS.Common.DTOs.Dashboard;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services.Student
{
    public sealed class StudentLeaderboardService : IStudentLeaderboardService
    {
        private readonly BlueSandsLMSDbContext _db;

        public StudentLeaderboardService(BlueSandsLMSDbContext db) => _db = db;

        public async Task<LeaderboardDto> GetAsync(Guid userId, string scope, int take, CancellationToken ct)
        {
            take = Math.Clamp(take <= 0 ? 10 : take, 1, 50);

            // resolve scope → filter user set
            IQueryable<Guid> population = _db.Users.Select(u => u.Id);

            var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                     ?? throw new InvalidOperationException("User not found");

            if (string.Equals(scope, "class", StringComparison.OrdinalIgnoreCase))
            {
                var classIds = await _db.Enrollments.Where(e => e.UserId == userId)
                                  .Select(e => e.ClassroomId).ToListAsync(ct);

                population = _db.Enrollments.Where(e => classIds.Contains(e.ClassroomId))
                              .Select(e => e.UserId).Distinct();
            }
            else if (string.Equals(scope, "school", StringComparison.OrdinalIgnoreCase))
            {
                if (me.SchoolId is Guid sid && sid != Guid.Empty)
                    population = _db.Users.Where(u => u.SchoolId == sid).Select(u => u.Id);
                else
                    population = _db.Users.Where(u => u.Id == userId).Select(u => u.Id); // fallback
            }
            // "national" (default/global) → keep all users

            var qa = await _db.QuizAttempts
                .Where(q => population.Contains(EF.Property<Guid>(q, "UserId")))
                .Select(q => new
                {
                    UserId = EF.Property<Guid>(q, "UserId"),
                    Score = (double?)EF.Property<double?>(q, "Score")
                          ?? (double?)EF.Property<decimal?>(q, "Percentage")
                          ?? (double?)EF.Property<double?>(q, "ScorePercent")
                          ?? 0.0
                })
                .ToListAsync(ct);

            var ranks = qa.GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, Score = g.Average(y => y.Score) })
                .OrderByDescending(x => x.Score)
                .Take(take)
                .ToList();

            var ids = ranks.Select(r => r.UserId).ToList();
            var users = await _db.Users.Where(u => ids.Contains(u.Id))
                          .Select(u => new { u.Id, u.FullName })
                          .ToListAsync(ct);

            var studentRanks = ranks.Select((r, i) =>
                new StudentRank(r.UserId,
                    users.FirstOrDefault(u => u.Id == r.UserId)?.FullName ?? "(Student)",
                    Math.Round(r.Score, 2),
                    i + 1)).ToList();

            // Teacher list not required on student leaderboard (return empty)
            return new LeaderboardDto(studentRanks, new List<TeacherRank>(), null);
        }
    }
}
