using System;
using Microsoft.Extensions.Caching.Memory;

namespace BlueSandsLMS.Application.Services.Infrastructure
{
    public interface ICacheInvalidator
    {
        void BustSchool(Guid schoolId);
        void BustSchoolScope(Guid schoolId, string scope); // e.g. "trends", "billing"
    }

    public sealed class CacheInvalidator : ICacheInvalidator
    {
        private readonly IMemoryCache _cache;
        public CacheInvalidator(IMemoryCache cache) => _cache = cache;

        // Keys used by SchoolAdmin V2 service
        private static string Overview(Guid s) => $"sa:overview:{s}";
        private static string Scope(Guid s, string scope) => $"sa:{scope}:{s}";

        public void BustSchool(Guid schoolId)
        {
            _cache.Remove(Overview(schoolId));
            _cache.Remove(Scope(schoolId, "trends"));
            _cache.Remove(Scope(schoolId, "performance"));
            _cache.Remove(Scope(schoolId, "teacher-activity"));
            _cache.Remove(Scope(schoolId, "experiments-courses"));
            _cache.Remove(Scope(schoolId, "system-metrics"));
            _cache.Remove(Scope(schoolId, "leaderboard"));
            _cache.Remove(Scope(schoolId, "billing"));
        }

        public void BustSchoolScope(Guid schoolId, string scope)
            => _cache.Remove(Scope(schoolId, scope));
    }
}
