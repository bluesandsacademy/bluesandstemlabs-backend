
using System;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BlueSandsLMS.Application.Services.Cache
{
    public sealed class CacheBustService : ICacheBustService
    {
        private readonly IMemoryCache _cache;
        public CacheBustService(IMemoryCache cache) => _cache = cache;

        public void InvalidateSchoolAdmin(Guid schoolId)
        {
            _cache.Remove($"sa:overview:{schoolId}");
            _cache.Remove($"sa:trends:{schoolId}");
            _cache.Remove($"sa:perf:{schoolId}");
            _cache.Remove($"sa:teacher:{schoolId}");
            _cache.Remove($"sa:expcourse:{schoolId}");
            _cache.Remove($"sa:system:{schoolId}");
            _cache.Remove($"sa:leader:{schoolId}");
            _cache.Remove($"sa:billing:{schoolId}");
        }

        public void InvalidateGlobal()
        {

            _cache.Remove("pricing:tiers");
            _cache.Remove("global:leaderboard:students");
            _cache.Remove("global:leaderboard:teachers");
            _cache.Remove("global:leaderboard:schools");
        }


        public void InvalidateUser(Guid userId)
        {
            _cache.Remove($"user:{userId}:overview");
            _cache.Remove($"user:{userId}:subs");
        }

        public void InvalidatePayment(string reference)
        {
            _cache.Remove($"payment:{reference}:verify");
        }
    }
}
