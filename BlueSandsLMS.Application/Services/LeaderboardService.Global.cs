using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;

namespace BlueSandsLMS.Application.Services
{
    public partial class LeaderboardService : IExtendedLeaderboardService
    {
        private static DateTime? LowerBound(string period)
        {
            var now = DateTime.UtcNow;
            return (period ?? "all").ToLowerInvariant() switch
            {
                "week"  => now.AddDays(-7),
                "month" => now.AddMonths(-1),
                _       => null
            };
        }

        public async Task<List<StudentRankDto>> GetGlobalStudentsAsync(string metric = "quiz", string period = "all", int top = 50)
        {
            var key = $"lb:global:students:{metric}:{period}:{top}";
            if (_cache.TryGetValue(key, out var boxed) && boxed is List<StudentRankDto> cached)
                return cached;

            DateTime? lb = LowerBound(period);


            List<(Guid Id, string FullName, double Value)> rows;
            switch (metric)
            {
                case "experiments":
                {
                    var raw = await (from e in _db.ExperimentLaunches.AsNoTracking()
                                      join u in _db.Users.AsNoTracking() on e.UserId equals u.Id
                                      where !lb.HasValue || e.DateCreated >= lb.Value
                                      select new { u.Id, u.FullName }).ToListAsync();
                    rows = raw.GroupBy(x => new { x.Id, x.FullName })
                        .Select(g => (g.Key.Id, g.Key.FullName, (double)g.LongCount()))
                        .ToList();
                    break;
                }
                case "time":
                {
                    var raw = await (from e in _db.ExperimentLaunches.AsNoTracking()
                                      join u in _db.Users.AsNoTracking() on e.UserId equals u.Id
                                      where !lb.HasValue || e.DateCreated >= lb.Value
                                      select new { u.Id, u.FullName, e.DurationSec }).ToListAsync();
                    rows = raw.GroupBy(x => new { x.Id, x.FullName })
                        .Select(g => (g.Key.Id, g.Key.FullName, g.Sum(x => x.DurationSec) / 60.0))
                        .ToList();
                    break;
                }
                case "badges":
                {
                    var raw = await (from b in _db.BadgeAwards.AsNoTracking()
                                      join u in _db.Users.AsNoTracking() on b.UserId equals u.Id
                                      where !lb.HasValue || b.DateCreated >= lb.Value
                                      select new { u.Id, u.FullName }).ToListAsync();
                    rows = raw.GroupBy(x => new { x.Id, x.FullName })
                        .Select(g => (g.Key.Id, g.Key.FullName, (double)g.LongCount()))
                        .ToList();
                    break;
                }
                default:
                {
                    var raw = await (from a in _db.QuizAttempts.AsNoTracking()
                                      join u in _db.Users.AsNoTracking() on a.UserId equals u.Id
                                      where !lb.HasValue || a.StartedAt >= lb.Value
                                      select new { u.Id, u.FullName, a.Score0to1 }).ToListAsync();
                    rows = raw.GroupBy(x => new { x.Id, x.FullName })
                        .Select(g => (g.Key.Id, g.Key.FullName, g.Average(x => (double)x.Score0to1 * 100.0)))
                        .ToList();
                    break;
                }
            }

            rows = rows.OrderByDescending(r => r.Value).ThenBy(r => r.FullName).Take(top).ToList();

            var entries = rows.Select((r, i) => new StudentRankDto(r.Id, r.FullName, r.Value, i + 1)).ToList();
            var dto = new GlobalLeaderboardResponse<StudentRankDto>("students", metric, period, DateTime.UtcNow, entries);
            _cache.Set(key, dto, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120) });
            return dto;
        }

        public async Task<GlobalLeaderboardResponse<TeacherRankDto>> GetGlobalTeachersAsync(string metric = "quiz", string period = "all", int top = 50)
{
    metric = (metric ?? "quiz").ToLowerInvariant();
    var key = $"lb:global:teachers:{metric}:{period}:{top}";
    if (_cache.TryGetValue(key, out var boxed) && boxed is GlobalLeaderboardResponse<TeacherRankDto> cached)
        return cached;

    DateTime? lb = LowerBound(period);


    var fromEnrollments = await _db.Enrollments.AsNoTracking()
        .Where(e => e.RoleInClass == ClassRole.Teacher)
        .Select(e => new { e.ClassroomId, TeacherId = e.UserId })
        .ToListAsync();

    var fromClassroomTeachers = await _db.ClassroomTeachers.AsNoTracking()
        .Select(t => new { t.ClassroomId, TeacherId = t.TeacherUserId })
        .ToListAsync();

    var teacherClassrooms = fromEnrollments.Concat(fromClassroomTeachers).Distinct().ToList();


    var classQuizAvg = await _db.QuizAttempts.AsNoTracking()
        .Where(a => !lb.HasValue || a.StartedAt >= lb.Value)
        .GroupBy(a => a.ClassroomId)
        .Select(g => new { ClassroomId = g.Key, Avg = g.Average(x => (double)x.Score0to1 * 100.0) })
        .ToListAsync();
    var classQuizAvgDict = classQuizAvg.Where(x => x.ClassroomId.HasValue)
        .ToDictionary(x => x.ClassroomId!.Value, x => x.Avg);


    var teacherScores = teacherClassrooms
        .GroupBy(tc => tc.TeacherId)
        .Select(g => new
        {
            TeacherId = g.Key,
            Score = g.Select(x => classQuizAvgDict.TryGetValue(x.ClassroomId, out var avg) ? avg : 0.0)
                     .DefaultIfEmpty(0.0)
                     .Average()
        })
        .ToList();


    var teacherIds = teacherScores.Select(t => t.TeacherId).ToList();
    var names = await _db.Users.AsNoTracking()
        .Where(u => teacherIds.Contains(u.Id))
        .Select(u => new { u.Id, u.FullName })
        .ToListAsync();
    var nameDict = names.ToDictionary(n => n.Id, n => n.FullName);

    var rows = teacherScores
        .Where(ts => nameDict.ContainsKey(ts.TeacherId))
        .Select(ts => new { TeacherId = ts.TeacherId, Name = nameDict[ts.TeacherId], ts.Score })
        .OrderByDescending(r => r.Score)
        .ThenBy(r => r.Name)
        .Take(top)
        .ToList();

    var entries = rows.Select((r, i) => new TeacherRankDto(r.TeacherId, r.Name, r.Score, i + 1)).ToList();

    var dto = new GlobalLeaderboardResponse<TeacherRankDto>(
        "teachers", metric, period, DateTime.UtcNow, entries
    );

    _cache.Set(key, dto, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120) });
    return dto;
}

        public async Task<GlobalLeaderboardResponse<SchoolRankDto>> GetGlobalSchoolsAsync(string metric = "quiz", string period = "all", int top = 50)
        {
            var key = $"lb:global:schools:{metric}:{period}:{top}";
            if (_cache.TryGetValue(key, out var boxed) && boxed is GlobalLeaderboardResponse<SchoolRankDto> cached)
                return cached;

            DateTime? lb = LowerBound(period);


            List<(Guid Id, string Name, double Value)> rows;
            switch (metric)
            {
                case "experiments":
                {
                    var raw = await (from e in _db.ExperimentLaunches.AsNoTracking()
                                      join c in _db.Classrooms.AsNoTracking() on e.ClassroomId equals c.Id
                                      join s in _db.Schools.AsNoTracking() on c.SchoolId equals s.Id
                                      where !lb.HasValue || e.DateCreated >= lb.Value
                                      select new { s.Id, s.Name }).ToListAsync();
                    rows = raw.GroupBy(x => new { x.Id, x.Name })
                        .Select(g => (g.Key.Id, g.Key.Name, (double)g.LongCount()))
                        .ToList();
                    break;
                }
                case "time":
                {
                    var raw = await (from e in _db.ExperimentLaunches.AsNoTracking()
                                      join c in _db.Classrooms.AsNoTracking() on e.ClassroomId equals c.Id
                                      join s in _db.Schools.AsNoTracking() on c.SchoolId equals s.Id
                                      where !lb.HasValue || e.DateCreated >= lb.Value
                                      select new { s.Id, s.Name, e.DurationSec }).ToListAsync();
                    rows = raw.GroupBy(x => new { x.Id, x.Name })
                        .Select(g => (g.Key.Id, g.Key.Name, g.Sum(x => x.DurationSec) / 60.0))
                        .ToList();
                    break;
                }
                default:
                {
                    var raw = await (from a in _db.QuizAttempts.AsNoTracking()
                                      join c in _db.Classrooms.AsNoTracking() on a.ClassroomId equals c.Id
                                      join s in _db.Schools.AsNoTracking() on c.SchoolId equals s.Id
                                      where !lb.HasValue || a.StartedAt >= lb.Value
                                      select new { s.Id, s.Name, a.Score0to1 }).ToListAsync();
                    rows = raw.GroupBy(x => new { x.Id, x.Name })
                        .Select(g => (g.Key.Id, g.Key.Name, g.Average(x => (double)x.Score0to1 * 100.0)))
                        .ToList();
                    break;
                }
            }

            rows = rows.OrderByDescending(r => r.Value).ThenBy(r => r.Name).Take(top).ToList();

            var entries = rows.Select((r, i) => new SchoolRankDto(r.Id, r.Name, r.Value, i + 1)).ToList();
            var dto = new GlobalLeaderboardResponse<SchoolRankDto>("schools", metric, period, DateTime.UtcNow, entries);
            _cache.Set(key, dto, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120) });
            return dto;
        }
    }
}
