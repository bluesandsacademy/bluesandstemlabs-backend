
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BlueSandsLMS.Application.Services
{

    public partial class LeaderboardService : ILeaderboardService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IMemoryCache _cache;

        public LeaderboardService(BlueSandsLMSDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public Task<LeaderboardResponseDto> GetClassAsync(Guid classId, string metric = "quiz", int top = 50) =>
            GetAsync(scope: "class", classId: classId, schoolId: null, metric: metric, top: top);

        public Task<LeaderboardResponseDto> GetSchoolAsync(Guid schoolId, string metric = "quiz", int top = 50) =>
            GetAsync(scope: "school", classId: null, schoolId: schoolId, metric: metric, top: top);

        public Task<LeaderboardResponseDto> GetGlobalAsync(string metric = "quiz", int top = 50) =>
            GetAsync(scope: "global", classId: null, schoolId: null, metric: metric, top: top);

        private async Task<LeaderboardResponseDto> GetAsync(string scope, Guid? classId, Guid? schoolId, string metric, int top)
        {
            var key = $"lb:{scope}:{classId}:{schoolId}:{metric}:{top}";
            if (_cache.TryGetValue(key, out LeaderboardResponseDto? cached) && cached is not null)
                return cached;

            IQueryable<Guid> userQuery = _db.Users.Select(u => u.Id);

            if (scope == "class" && classId.HasValue)
            {
                userQuery = _db.Enrollments
                    .Where(e => e.ClassroomId == classId.Value && e.RoleInClass == Core.Entities.ClassRole.Student)
                    .Select(e => e.UserId);
            }
            else if (scope == "school" && schoolId.HasValue)
            {
                userQuery = _db.Users.Where(u => u.SchoolId == schoolId.Value && u.Role != null && u.Role.Name == "Student")
                                     .Select(u => u.Id);
            }

            var userIds = await userQuery.Distinct().ToListAsync();


            IQueryable<(Guid UserId, decimal Score)> metricQuery = metric.ToLower() switch
            {
                "quiz" => (
                    from q in _db.QuizAttempts
                    where userIds.Contains(q.UserId) && q.CompletedAt != null
                    group q by q.UserId into g
                    select new ValueTuple<Guid, decimal>(
                        g.Key,
                        (g.Average(x => (decimal?)x.Score0to1) ?? 0m) * 100m
                    )
                ),

                "experiments" => (
                    from e in _db.ExperimentLaunches
                    where userIds.Contains(e.UserId) && e.Completed
                    group e by e.UserId into g
                    select new ValueTuple<Guid, decimal>(g.Key, (decimal)g.Count())
                ),

                "time" => (
                    from e in _db.ExperimentLaunches
                    where userIds.Contains(e.UserId)
                    group e by e.UserId into g
                    select new ValueTuple<Guid, decimal>(g.Key, (decimal)(g.Sum(x => (int?)x.DurationSec) ?? 0) / 60m)
                ),

                "badges" => (
                    from b in _db.BadgeAwards
                    where userIds.Contains(b.UserId)
                    group b by b.UserId into g
                    select new ValueTuple<Guid, decimal>(g.Key, (decimal)g.Count())
                ),

                _ => (
                    from q in _db.QuizAttempts
                    where userIds.Contains(q.UserId) && q.CompletedAt != null
                    group q by q.UserId into g
                    select new ValueTuple<Guid, decimal>(
                        g.Key,
                        (g.Average(x => (decimal?)x.Score0to1) ?? 0m) * 100m
                    )
                )
            };

            var rows = await metricQuery
                .OrderByDescending(x => x.Score)
                .Take(top)
                .ToListAsync();

            var users = await _db.Users
                .Where(u => rows.Select(r => r.UserId).Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            var entries = rows
                .Select((r, idx) =>
                {
                    var u = users.FirstOrDefault(x => x.Id == r.UserId);
                    return new LeaderboardEntryDto(
                        r.UserId, u?.FullName ?? "Student", u?.Email ?? "", Math.Round(r.Score, 2), idx + 1
                    );
                })
                .ToList();

            var dto = new LeaderboardResponseDto(
                Scope: scope,
                ClassId: classId,
                SchoolId: schoolId,
                Metric: metric.ToLower(),
                GeneratedAtUtc: DateTime.UtcNow,
                Entries: entries
            );

            _cache.Set(key, dto, TimeSpan.FromMinutes(5));
            return dto;
        }
    }
}
