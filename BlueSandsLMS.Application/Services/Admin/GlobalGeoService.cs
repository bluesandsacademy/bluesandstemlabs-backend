using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;
using BlueSandsLMS.Infrastructure;

namespace BlueSandsLMS.Application.Services.Admin
{
    public sealed class GlobalGeoService : IGlobalGeoService
    {
        private readonly BlueSandsLMSDbContext _db;
        
        public GlobalGeoService(BlueSandsLMSDbContext db) => _db = db;

        public async Task<GeoAdvancedDto> GetAsync(string scope, string? country, string? state, CancellationToken ct = default)
        {
            scope = (scope ?? "country").ToLowerInvariant();
            if (scope is not ("country" or "state" or "lga")) scope = "country";


            var schoolsQ = _db.Schools.AsNoTracking().Select(s => new {
                s.Id, s.Country, s.State, s.Lga
            });

            if (!string.IsNullOrWhiteSpace(country)) schoolsQ = schoolsQ.Where(s => s.Country == country);
            if (!string.IsNullOrWhiteSpace(state))   schoolsQ = schoolsQ.Where(s => s.State == state);

            var schoolsList = await schoolsQ.ToListAsync(ct);
            var schoolIds = schoolsList.Select(s => s.Id).ToHashSet();


            var classToSchool = await _db.Classrooms.AsNoTracking()
                .Where(c => schoolIds.Contains(c.SchoolId))
                .Select(c => new { c.Id, c.SchoolId })
                .ToDictionaryAsync(x => x.Id, x => x.SchoolId, ct);


            var exps = await _db.ExperimentLaunches.AsNoTracking()
                .Where(e => e.ClassroomId != null && classToSchool.ContainsKey(e.ClassroomId.Value))
                .GroupBy(e => classToSchool[e.ClassroomId!.Value])
                .Select(g => new { SchoolId = g.Key, Count = g.LongCount() })
                .ToDictionaryAsync(x => x.SchoolId, x => x.Count, ct);

            var quizzes = await _db.QuizAttempts.AsNoTracking()
                .Where(q => q.ClassroomId != null && classToSchool.ContainsKey(q.ClassroomId.Value))
                .GroupBy(q => classToSchool[q.ClassroomId!.Value])
                .Select(g => new { SchoolId = g.Key, Count = g.LongCount() })
                .ToDictionaryAsync(x => x.SchoolId, x => x.Count, ct);

            var users = await _db.Users.AsNoTracking()
                .Where(u => u.SchoolId != null && schoolIds.Contains(u.SchoolId.Value))
                .GroupBy(u => u.SchoolId!.Value)
                .Select(g => new { SchoolId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SchoolId, x => x.Count, ct);


            IEnumerable<IGrouping<string?, dynamic>> grouped = scope switch
            {
                "state" => schoolsList.GroupBy(s => s.State),
                "lga"   => schoolsList.GroupBy(s => s.Lga),
                _       => schoolsList.GroupBy(s => s.Country)
            };

            var rows = new List<GeoRow>();
            foreach (var g in grouped)
            {
                int schoolCount = 0, userCount = 0; long expCount = 0, quizCount = 0;
                foreach (var s in g)
                {
                    schoolCount++;


                    if (users.TryGetValue(s.Id, out int uc)) userCount += uc;
                    if (exps.TryGetValue(s.Id, out long ec)) expCount += ec;
                    if (quizzes.TryGetValue(s.Id, out long qc)) quizCount += qc;
                }

                var key = string.IsNullOrWhiteSpace(g.Key) ? "(Unknown)" : g.Key!;
                rows.Add(new GeoRow(key, schoolCount, userCount, expCount, quizCount));
            }

            rows = rows.OrderByDescending(r => r.Users).ThenBy(r => r.Key).ToList();
            return new GeoAdvancedDto(scope, country, state, rows.ToArray(), DateTime.UtcNow);
        }
    }
}