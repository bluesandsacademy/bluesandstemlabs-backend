using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;
using BlueSandsLMS.Infrastructure;

namespace BlueSandsLMS.Application.Services.Admin
{
    public sealed class GlobalInsightsService : IGlobalInsightsService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IMemoryCache _cache;
        
        public GlobalInsightsService(BlueSandsLMSDbContext db, IMemoryCache cache) 
        { 
            _db = db; 
            _cache = cache; 
        }

        public async Task<GlobalAiInsightsDto> GetAsync(CancellationToken ct = default)
        {
            const string key = "ga:insights:v1";
            if (_cache.TryGetValue(key, out var boxed) && boxed is GlobalAiInsightsDto cached) 
                return cached;

            var now = DateTime.UtcNow;
            var since30 = now.AddDays(-30);


            var experimentNames = await _db.ExperimentLaunches.AsNoTracking()
                .Select(e => e.ExperimentName)
                .ToListAsync(ct);
            var topExps = experimentNames
                .GroupBy(name => name)
                .Select(g => new LabeledValue(g.Key, g.LongCount()))
                .OrderByDescending(x => x.Value).ThenBy(x => x.Label)
                .Take(10).ToList();


            var classSubjects = await _db.Classrooms.AsNoTracking()
                .Select(c => new { c.Id, c.Subject })
                .ToListAsync(ct);

            var expByClass = await _db.ExperimentLaunches.AsNoTracking()
                .GroupBy(e => e.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Cnt = g.LongCount() })
                .ToListAsync(ct);

            var quizByClass = await _db.QuizAttempts.AsNoTracking()
                .GroupBy(q => q.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Cnt = g.LongCount() })
                .ToListAsync(ct);

            var expByClassDict = expByClass.Where(x => x.ClassroomId.HasValue)
                .ToDictionary(x => x.ClassroomId!.Value, x => x.Cnt);
            var quizByClassDict = quizByClass.Where(x => x.ClassroomId.HasValue)
                .ToDictionary(x => x.ClassroomId!.Value, x => x.Cnt);

            var topSubjects = classSubjects
                .GroupBy(c => c.Subject)
                .Select(g => new LabeledValue(
                    g.Key,
                    g.Sum(c =>
                        (expByClassDict.TryGetValue(c.Id, out var ec) ? ec : 0) +
                        (quizByClassDict.TryGetValue(c.Id, out var qc) ? qc : 0))
                ))
                .OrderByDescending(x => x.Value).ThenBy(x => x.Label)
                .Take(10).ToList();


            var hoursExp = await _db.ExperimentLaunches.AsNoTracking()
                .Where(e => e.DateCreated >= since30)
                .Select(e => e.DateCreated.Hour)
                .ToListAsync(ct);
            var hoursQuiz = await _db.QuizAttempts.AsNoTracking()
                .Where(q => q.StartedAt >= since30)
                .Select(q => q.StartedAt.Hour)
                .ToListAsync(ct);

            var hourCounts = new long[24];
            foreach (var h in hoursExp) hourCounts[h]++;
            foreach (var h in hoursQuiz) hourCounts[h]++;

            var peak = Enumerable.Range(0, 24).Select(h => new HourBucket(h, hourCounts[h])).ToList();


            var quiz30 = await _db.QuizAttempts.AsNoTracking()
                .Where(q => q.StartedAt >= since30)
                .Select(q => (double)q.Score0to1)
                .ToListAsync(ct);
            var avgQuizPercent = quiz30.Count == 0 ? 0 : quiz30.Average() * 100.0;


            var activeUsers = await (
                from u in _db.Users.AsNoTracking()
                where
                    _db.QuizAttempts.Any(q => q.UserId == u.Id && q.StartedAt >= since30) ||
                    _db.ExperimentLaunches.Any(e => e.UserId == u.Id && e.DateCreated >= since30)
                select u.Id
            ).Distinct().CountAsync(ct);

            var dto = new GlobalAiInsightsDto(now, topExps, topSubjects, peak, avgQuizPercent, activeUsers);
            _cache.Set(key, dto, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            return dto;
        }
    }
}