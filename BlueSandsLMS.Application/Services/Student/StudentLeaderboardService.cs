using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.DTOs.Dashboard;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
 using DashboardLeaderboardEntry = BlueSandsLMS.Common.DTOs.LeaderboardEntry;

namespace BlueSandsLMS.Application.Services.Student
{
   

    public sealed class StudentLeaderboardService : IStudentLeaderboardService
    {
        private readonly BlueSandsLMSDbContext _db;
        
        public StudentLeaderboardService(BlueSandsLMSDbContext db) => _db = db;


        public async Task<LeaderboardDto> GetLeaderboardAsync(Guid studentId, string scope, CancellationToken ct)
        {
            int take = 10;

            take = Math.Clamp(take, 1, 50);

            IQueryable<Guid> population = _db.Users.Select(u => u.Id);
            
            var me = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == studentId, ct)
                     ?? throw new InvalidOperationException("User not found");

            if (string.Equals(scope, "class", StringComparison.OrdinalIgnoreCase))
            {
                var classIds = await _db.Enrollments.Where(e => e.UserId == studentId)
                                  .Select(e => e.ClassroomId).ToListAsync(ct);
                population = _db.Enrollments.Where(e => classIds.Contains(e.ClassroomId)).Select(e => e.UserId).Distinct();
            }
            else if (string.Equals(scope, "school", StringComparison.OrdinalIgnoreCase))
            {
                if (me.SchoolId is Guid sid && sid != Guid.Empty)
                    population = _db.Users.Where(u => u.SchoolId == sid).Select(u => u.Id);
                else
                    population = _db.Users.Where(u => u.Id == studentId).Select(u => u.Id);
            }

            var qa = await _db.QuizAttempts
                .AsNoTracking()
                .Where(q => population.Contains(q.UserId))
                .Select(q => new { q.UserId, Score = (double)(q.Score0to1 * 100m) })
                .ToListAsync(ct);

            var top = qa.GroupBy(x => x.UserId)
                        .Select(g => new { UserId = g.Key, Points = g.Average(y => y.Score) })
                        .OrderByDescending(x => x.Points)
                        .Take(take)
                        .ToList();

            var ids = top.Select(t => t.UserId).ToList();
            var users = await _db.Users.Where(u => ids.Contains(u.Id))
                          .Select(u => new { u.Id, u.FullName })
                          .ToListAsync(ct);

            var entries = top.Select((r, i) =>

                new DashboardLeaderboardEntry(
                    r.UserId,
                    users.FirstOrDefault(u => u.Id == r.UserId)?.FullName ?? "(Student)",
                    null,
                    (int)Math.Round(r.Points),
                    i + 1))
                .ToList();

            return new LeaderboardDto(entries, entries.Count, 1, entries.Count);
        }
    }
}