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
                "week" => now.AddDays(-7),
                "month" => now.AddMonths(-1),
                _ => null
            };
        }

        public async Task<List<StudentRankDto>> GetGlobalStudentsAsync(string metric = "quiz", string period = "all", int top = 50)
        {
            var key = $"lb:global:students:{metric}:{period}:{top}";
            if (_cache.TryGetValue(key, out var boxed) && boxed is List<StudentRankDto> cached)
                return cached;

            DateTime? lb = LowerBound(period);


            List<(Guid Id, string FullName, string SchoolName, string Country, double Value)> rows;
            switch (metric)
            {
                case "experiments":
                    {
                        var raw = await (from e in _db.ExperimentLaunches.AsNoTracking()
                                         join u in _db.Users.AsNoTracking() on e.UserId equals u.Id
                                         join s in _db.Schools.AsNoTracking() on u.SchoolId equals s.Id into schoolJoin
                                         from s in schoolJoin.DefaultIfEmpty()
                                         where !lb.HasValue || e.DateCreated >= lb.Value
                                         select new { u.Id, u.FullName, SchoolName = s != null ? s.Name : "", Country = s != null ? s.Country : "" }).ToListAsync();
                        rows = raw.GroupBy(x => new { x.Id, x.FullName, x.SchoolName, x.Country })
                            .Select(g => (g.Key.Id, g.Key.FullName, g.Key.SchoolName, g.Key.Country, (double)g.LongCount()))
                            .ToList();
                        break;
                    }
                case "time":
                    {
                        var raw = await (from e in _db.ExperimentLaunches.AsNoTracking()
                                         join u in _db.Users.AsNoTracking() on e.UserId equals u.Id
                                         join s in _db.Schools.AsNoTracking() on u.SchoolId equals s.Id into schoolJoin
                                         from s in schoolJoin.DefaultIfEmpty()
                                         where !lb.HasValue || e.DateCreated >= lb.Value
                                         select new { u.Id, u.FullName, SchoolName = s != null ? s.Name : "", Country = s != null ? s.Country : "", e.DurationSec }).ToListAsync();
                        rows = raw.GroupBy(x => new { x.Id, x.FullName, x.SchoolName, x.Country })
                            .Select(g => (g.Key.Id, g.Key.FullName, g.Key.SchoolName, g.Key.Country, g.Sum(x => x.DurationSec) / 60.0))
                            .ToList();
                        break;
                    }
                case "badges":
                    {
                        var raw = await (from b in _db.BadgeAwards.AsNoTracking()
                                         join u in _db.Users.AsNoTracking() on b.UserId equals u.Id
                                         join s in _db.Schools.AsNoTracking() on u.SchoolId equals s.Id into schoolJoin
                                         from s in schoolJoin.DefaultIfEmpty()
                                         where !lb.HasValue || b.DateCreated >= lb.Value
                                         select new { u.Id, u.FullName, SchoolName = s != null ? s.Name : "", Country = s != null ? s.Country : "" }).ToListAsync();
                        rows = raw.GroupBy(x => new { x.Id, x.FullName, x.SchoolName, x.Country })
                            .Select(g => (g.Key.Id, g.Key.FullName, g.Key.SchoolName, g.Key.Country, (double)g.LongCount()))
                            .ToList();
                        break;
                    }
                default:
                    {
                        var raw = await (from a in _db.QuizAttempts.AsNoTracking()
                                         join u in _db.Users.AsNoTracking() on a.UserId equals u.Id
                                         join s in _db.Schools.AsNoTracking() on u.SchoolId equals s.Id into schoolJoin
                                         from s in schoolJoin.DefaultIfEmpty()
                                         where !lb.HasValue || a.StartedAt >= lb.Value
                                         select new { u.Id, u.FullName, SchoolName = s != null ? s.Name : "", Country = s != null ? s.Country : "", a.Score0to1 }).ToListAsync();
                        rows = raw.GroupBy(x => new { x.Id, x.FullName, x.SchoolName, x.Country })
                            .Select(g => (g.Key.Id, g.Key.FullName, g.Key.SchoolName, g.Key.Country, g.Average(x => (double)x.Score0to1 * 100.0)))
                            .ToList();
                        break;
                    }
            }

            rows = rows.OrderByDescending(r => r.Value).ThenBy(r => r.FullName).Take(top).ToList();

            var entries = rows.Select((r, i) => new StudentRankDto(
                r.FullName,
                r.SchoolName,
                r.Country,
                metric == "experiments" ? ((int)r.Value).ToString() : "0",
                (int)r.Value,
                Math.Round(r.Value, 2)
            )).ToList();
            _cache.Set(key, entries, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120) });
            return entries;
        }

        public async Task<List<TeacherRankDto>> GetGlobalTeachersAsync(string metric = "quiz", string period = "all", int top = 50)
        {
            metric = (metric ?? "quiz").ToLowerInvariant();
            var key = $"lb:global:teachers:{metric}:{period}:{top}";
            if (_cache.TryGetValue(key, out var boxed) && boxed is List<TeacherRankDto> cached)
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

            // Get student counts per classroom
            var classStudentCounts = await _db.Enrollments.AsNoTracking()
                .Where(e => e.RoleInClass == ClassRole.Student)
                .GroupBy(e => e.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassroomId, x => x.Count);

            // Get subjects per classroom
            var classSubjects = await _db.Classrooms.AsNoTracking()
                .Select(c => new { c.Id, c.Subject })
                .ToDictionaryAsync(x => x.Id, x => x.Subject);

            var teacherScores = teacherClassrooms
                .GroupBy(tc => tc.TeacherId)
                .Select(g => new
                {
                    TeacherId = g.Key,
                    Score = g.Select(x => classQuizAvgDict.TryGetValue(x.ClassroomId, out var avg) ? avg : 0.0)
                             .DefaultIfEmpty(0.0)
                             .Average(),
                    Students = g.Sum(x => classStudentCounts.TryGetValue(x.ClassroomId, out var c) ? c : 0),
                    Subject = g.Select(x => classSubjects.TryGetValue(x.ClassroomId, out var s) ? s : "")
                               .Where(s => !string.IsNullOrEmpty(s))
                               .FirstOrDefault() ?? ""
                })
                .ToList();


            var teacherIds = teacherScores.Select(t => t.TeacherId).ToList();
            var teacherUsers = await _db.Users.AsNoTracking()
                .Where(u => teacherIds.Contains(u.Id))
                .Join(_db.Schools.AsNoTracking(), u => u.SchoolId, s => s.Id, (u, s) => new { u.Id, u.FullName, SchoolName = s.Name, s.Country })
                .ToListAsync();
            var teacherInfoDict = teacherUsers.ToDictionary(n => n.Id);

            var rows = teacherScores
                .Where(ts => teacherInfoDict.ContainsKey(ts.TeacherId))
                .Select(ts =>
                {
                    var info = teacherInfoDict[ts.TeacherId];
                    return new { ts.TeacherId, info.FullName, info.SchoolName, info.Country, ts.Subject, ts.Students, ts.Score };
                })
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.FullName)
                .Take(top)
                .ToList();

            var entries = rows.Select(r => new TeacherRankDto(
                r.FullName,
                r.SchoolName,
                r.Country,
                r.Subject,
                r.Students.ToString(),
                $"{r.Score:F1}%",
                (int)r.Score,
                Math.Round(r.Score, 2)
            )).ToList();

            _cache.Set(key, entries, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120) });
            return entries;
        }

        public async Task<List<SchoolRankDto>> GetGlobalSchoolsAsync(string metric = "quiz", string period = "all", int top = 50)
        {
            var key = $"lb:global:schools:{metric}:{period}:{top}";
            if (_cache.TryGetValue(key, out var boxed) && boxed is List<SchoolRankDto> cached)
                return cached;

            DateTime? lb = LowerBound(period);


            List<(Guid Id, string Name, string Country, double Value)> rows;
            switch (metric)
            {
                case "experiments":
                    {
                        var raw = await (from e in _db.ExperimentLaunches.AsNoTracking()
                                         join c in _db.Classrooms.AsNoTracking() on e.ClassroomId equals c.Id
                                         join s in _db.Schools.AsNoTracking() on c.SchoolId equals s.Id
                                         where !lb.HasValue || e.DateCreated >= lb.Value
                                         select new { s.Id, s.Name, s.Country }).ToListAsync();
                        rows = raw.GroupBy(x => new { x.Id, x.Name, x.Country })
                            .Select(g => (g.Key.Id, g.Key.Name, g.Key.Country, (double)g.LongCount()))
                            .ToList();
                        break;
                    }
                case "time":
                    {
                        var raw = await (from e in _db.ExperimentLaunches.AsNoTracking()
                                         join c in _db.Classrooms.AsNoTracking() on e.ClassroomId equals c.Id
                                         join s in _db.Schools.AsNoTracking() on c.SchoolId equals s.Id
                                         where !lb.HasValue || e.DateCreated >= lb.Value
                                         select new { s.Id, s.Name, s.Country, e.DurationSec }).ToListAsync();
                        rows = raw.GroupBy(x => new { x.Id, x.Name, x.Country })
                            .Select(g => (g.Key.Id, g.Key.Name, g.Key.Country, g.Sum(x => x.DurationSec) / 60.0))
                            .ToList();
                        break;
                    }
                default:
                    {
                        var raw = await (from a in _db.QuizAttempts.AsNoTracking()
                                         join c in _db.Classrooms.AsNoTracking() on a.ClassroomId equals c.Id
                                         join s in _db.Schools.AsNoTracking() on c.SchoolId equals s.Id
                                         where !lb.HasValue || a.StartedAt >= lb.Value
                                         select new { s.Id, s.Name, s.Country, a.Score0to1 }).ToListAsync();
                        rows = raw.GroupBy(x => new { x.Id, x.Name, x.Country })
                            .Select(g => (g.Key.Id, g.Key.Name, g.Key.Country, g.Average(x => (double)x.Score0to1 * 100.0)))
                            .ToList();
                        break;
                    }
            }

            rows = rows.OrderByDescending(r => r.Value).ThenBy(r => r.Name).Take(top).ToList();

            // Get student counts and experiment counts per school
            var schoolIds = rows.Select(r => r.Id).ToList();

            var schoolStudentCounts = await _db.Users.AsNoTracking()
                .Where(u => u.SchoolId.HasValue && schoolIds.Contains(u.SchoolId.Value))
                .GroupBy(u => u.SchoolId!.Value)
                .Select(g => new { SchoolId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SchoolId, x => x.Count);

            var schoolExperimentCounts = await _db.ExperimentLaunches.AsNoTracking()
                .Join(_db.Classrooms.AsNoTracking(), e => e.ClassroomId, c => c.Id, (e, c) => new { e, c.SchoolId })
                .Where(x => schoolIds.Contains(x.SchoolId))
                .GroupBy(x => x.SchoolId)
                .Select(g => new { SchoolId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SchoolId, x => x.Count);

            var entries = rows.Select((r, i) => new SchoolRankDto(
                r.Name,
                r.Country,
                schoolStudentCounts.TryGetValue(r.Id, out var sc) ? sc : 0,
                schoolExperimentCounts.TryGetValue(r.Id, out var ec) ? ec : 0,
                (int)r.Value,
                Math.Round(r.Value, 2)
            )).ToList();

            _cache.Set(key, entries, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120) });
            return entries;
        }
    }
}