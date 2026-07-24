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
using BlueSandsLMS.Core.Entities;

namespace BlueSandsLMS.Application.Services.Admin
{
    public sealed class GlobalAdminService : IGlobalAdminService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IMemoryCache _cache;

        public GlobalAdminService(BlueSandsLMSDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }


        public async Task<GlobalAdminTotalsDto> GetTotalsAsync(CancellationToken ct = default)
{
    const string cacheKey = "ga:totals:v5";
    if (_cache.TryGetValue(cacheKey, out var boxed) && boxed is GlobalAdminTotalsDto cached)
        return cached;

    var nowUtc  = DateTime.UtcNow;
    var since30 = nowUtc.AddDays(-30);




    var totalUsers         = await _db.Users.CountAsync(ct);
    var activeUsers30d     = await _db.Users.CountAsync(u => u.LastLogin != null && u.LastLogin >= since30, ct);
    var totalSchools       = await _db.Schools.CountAsync(ct);
    var experimentAttempts = await _db.ExperimentLaunches.LongCountAsync(ct);
    var quizAttempts       = await _db.QuizAttempts.LongCountAsync(ct);
    var totalLabTimeSec    = await _db.ExperimentLaunches.SumAsync(e => (long)e.DurationSec, ct);
    var revenue            = await _db.Payments.Where(p => p.Status == PaymentStatus.Paid)
                                               .SumAsync(p => (decimal?)p.Total, ct);
    var activeSubs         = await _db.Subscriptions.CountAsync(s => s.Active, ct);


    var maleUsers   = await _db.Users.CountAsync(u => u.Gender != null && u.Gender.ToLower() == "male", ct);
    var femaleUsers = await _db.Users.CountAsync(u => u.Gender != null && u.Gender.ToLower() == "female", ct);


    var offlineUsers = await _db.ExperimentLaunches
        .Where(e => e.Mode != null && e.Mode.ToLower() == "offline"
                    && e.DateCreated >= since30 && e.UserId != Guid.Empty)
        .Select(e => e.UserId)
        .Distinct()
        .CountAsync(ct);


    var totalPayments     = await _db.Payments.CountAsync(p => p.Status == PaymentStatus.Paid, ct);
    var totalStemCourses  = await _db.PhETSimulations.CountAsync(ct);
    var totalQuizScores   = await _db.QuizAttempts.SumAsync(q => (double)q.Score0to1, ct);
    var totalIls          = await _db.InteractiveLearningSpaces.CountAsync(ct);


    var activeSubSchoolIds = await _db.Subscriptions
        .Where(s => s.Active && s.SchoolId != Guid.Empty)
        .Select(s => s.SchoolId)
        .Distinct()
        .ToListAsync(ct);


    var activeSubUserIds = await _db.Subscriptions
        .Where(s => s.Active && s.UserId != null && s.UserId != Guid.Empty)
        .Select(s => s.UserId!.Value)
        .Distinct()
        .ToListAsync(ct);


    List<Guid> usersInSubbedSchoolsIds = activeSubSchoolIds.Count == 0
        ? new List<Guid>()
        : await _db.Users.AsNoTracking()
            .Where(u => u.SchoolId.HasValue && activeSubSchoolIds.Contains(u.SchoolId.Value))
            .Select(u => u.Id)
            .ToListAsync(ct);


    var subscribedUsers = new HashSet<Guid>(usersInSubbedSchoolsIds);
    foreach (var uid in activeSubUserIds) subscribedUsers.Add(uid);
    var totalSubscribedUsers = subscribedUsers.Count;

    var dto = new GlobalAdminTotalsDto(
        TotalUsers:              totalUsers,
        ActiveUsers30d:          activeUsers30d,
        TotalSchools:            totalSchools,
        TotalExperimentAttempts: experimentAttempts,
        TotalQuizAttempts:       quizAttempts,
        TotalLabTimeMinutes:     totalLabTimeSec / 60L,
        TotalRevenueNGN:         revenue ?? 0m,
        ActiveSubscriptions:     activeSubs,


        TotalPayments:           totalPayments,
        TotalStemCourses:        totalStemCourses,
        TotalQuizScores:         totalQuizScores,
        TotalSubscribedUsers:    totalSubscribedUsers,
        MaleUsers:               maleUsers,
        FemaleUsers:             femaleUsers,
        OfflineUsers:            offlineUsers,
        TotalIls:                totalIls,

        GeneratedAtUtc:          nowUtc
    );

    _cache.Set(cacheKey, dto, new MemoryCacheEntryOptions {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120)
    });

    return dto;
}



        public async Task<GrowthSeriesDto> GetGrowthAsync(string metric, string period, int points, CancellationToken ct = default)
        {
            metric = (metric ?? "users").ToLowerInvariant();
            period = (period ?? "day").ToLowerInvariant();
            if (points <= 0 || points > 400) points = 30;

            DateTime now = DateTime.UtcNow;

            IQueryable<DateTime> source = metric switch
            {
                "users"       => _db.Users.AsNoTracking().Select(x => x.DateCreated),
                "schools"     => _db.Schools.AsNoTracking().Select(x => x.DateCreated),
                "experiments" => _db.ExperimentLaunches.AsNoTracking().Select(x => x.DateCreated),
                "quizzes"     => _db.QuizAttempts.AsNoTracking().Select(x => x.StartedAt),
                "payments"    => _db.Payments.AsNoTracking().Select(x => x.DateCreated),
                _             => _db.Users.AsNoTracking().Select(x => x.DateCreated)
            };

            Func<DateTime, DateTime> bucket = period switch
            {
                "week"  => (d) => StartOfWeek(d),
                "month" => (d) => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                _       => (d) => d.Date
            };

            DateTime end = bucket(now);
            DateTime start = period switch
            {
                "week"  => end.AddDays(-7 * (points - 1)),
                "month" => end.AddMonths(-(points - 1)),
                _       => end.AddDays(-(points - 1))
            };

            var grouped = source
                .Where(d => d >= start && d <= end)
                .AsEnumerable()
                .GroupBy(d => bucket(d))
                .ToDictionary(g => g.Key, g => (long)g.LongCount());

            var pts = new List<SeriesPoint>(points);
            var cursor = start;
            for (int i = 0; i < points; i++)
            {
                grouped.TryGetValue(cursor, out var v);
                pts.Add(new SeriesPoint(cursor, v));
                cursor = period switch
                {
                    "week"  => cursor.AddDays(7),
                    "month" => cursor.AddMonths(1),
                    _       => cursor.AddDays(1)
                };
            }

            return new GrowthSeriesDto(metric, period, pts.ToArray());
        }

        private static DateTime StartOfWeek(DateTime d)
        {
            int diff = ((int)d.DayOfWeek + 6) % 7;
            var date = d.Date.AddDays(-diff);
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }


        public async Task<GeoUsageDto> GetGeoUsageAsync(CancellationToken ct = default)
        {
            var schools = await _db.Schools.AsNoTracking()
                                           .Select(s => new { s.Id, Country = s.Country ?? "" })
                                           .ToListAsync(ct);

            var byCountry = schools.GroupBy(s => s.Country)
                                   .ToDictionary(g => g.Key, g => new { Schools = g.Count(), SchoolIds = g.Select(x => x.Id).ToHashSet() });

            var classrooms = await _db.Classrooms.AsNoTracking().Select(c => new { c.Id, c.SchoolId }).ToListAsync(ct);
            var classToSchool = classrooms.ToDictionary(x => x.Id, x => x.SchoolId);

            var exps = await _db.ExperimentLaunches.AsNoTracking()
                .GroupBy(e => e.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Count = g.LongCount() })
                .ToListAsync(ct);

            var quiz = await _db.QuizAttempts.AsNoTracking()
                .GroupBy(q => q.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Count = g.LongCount() })
                .ToListAsync(ct);

            var expBySchool = new Dictionary<Guid, long>();
            foreach (var e in exps)
                if (e.ClassroomId.HasValue && classToSchool.TryGetValue(e.ClassroomId.Value, out var sid))
                    expBySchool[sid] = (expBySchool.TryGetValue(sid, out var v) ? v : 0) + e.Count;

            var quizBySchool = new Dictionary<Guid, long>();
            foreach (var q in quiz)
                if (q.ClassroomId.HasValue && classToSchool.TryGetValue(q.ClassroomId.Value, out var sid))
                    quizBySchool[sid] = (quizBySchool.TryGetValue(sid, out var v) ? v : 0) + q.Count;

            var usersBySchool = await _db.Users.AsNoTracking()
                .Where(u => u.SchoolId.HasValue)
                .GroupBy(u => u.SchoolId!.Value)
                .Select(g => new { SchoolId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SchoolId, x => x.Count, ct);

            var rows = new List<GeoUsageRow>();
            foreach (var (country, info) in byCountry)
            {
                int usersCount = 0; long expCount = 0; long quizCount = 0;
                foreach (var sid in info.SchoolIds)
                {
                    usersCount += usersBySchool.TryGetValue(sid, out var uc) ? uc : 0;
                    expCount   += expBySchool.TryGetValue(sid, out var ec) ? ec : 0;
                    quizCount  += quizBySchool.TryGetValue(sid, out var qc) ? qc : 0;
                }
                rows.Add(new GeoUsageRow(country, info.Schools, usersCount, expCount, quizCount));
            }

            return new GeoUsageDto(rows.OrderByDescending(r => r.Users).ToArray(), DateTime.UtcNow);
        }


        public async Task<PagedResult<GlobalAdminUserRowDto>> SearchUsersAsync(UserQuery query, CancellationToken ct = default)
        {
            if (query.Page <= 0) query = query with { Page = 1 };
            if (query.PageSize is < 1 or > 200) query = query with { PageSize = 20 };

            var q = _db.Users.AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.School)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var needle = query.Q.Trim().ToLower();
                q = q.Where(u =>
                    u.Email.ToLower().Contains(needle) ||
                    u.FullName.ToLower().Contains(needle) ||
                    (u.Phone != null && u.Phone.ToLower().Contains(needle)));
            }

            if (!string.IsNullOrWhiteSpace(query.Role))
            {
                var desired = query.Role.Trim();
                q = q.Where(u => u.Role != null && u.Role.Name == desired);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var s = query.Status.Trim().ToLowerInvariant();
                if (s == "active") q = q.Where(u => u.IsActive);
                else if (s == "inactive") q = q.Where(u => !u.IsActive);
            }

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(u => u.DateCreated)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(u => new GlobalAdminUserRowDto(
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role != null ? u.Role.Name : "Unknown",
                    u.SchoolId,
                    u.School != null ? u.School.Name : null,
                    u.IsActive,
                    u.IsEmailVerified,
                    u.DateCreated,
                    u.LastLogin
                ))
                .ToListAsync(ct);

            return new PagedResult<GlobalAdminUserRowDto>(query.Page, query.PageSize, total, items);
        }

        public async Task<GlobalAdminUserRowDto?> GetUserAsync(Guid userId, CancellationToken ct = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.School)
                .Where(u => u.Id == userId)
                .Select(u => new GlobalAdminUserRowDto(
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role != null ? u.Role.Name : "Unknown",
                    u.SchoolId,
                    u.School != null ? u.School.Name : null,
                    u.IsActive,
                    u.IsEmailVerified,
                    u.DateCreated,
                    u.LastLogin
                ))
                .FirstOrDefaultAsync(ct);
        }

        public async Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
                ?? throw new InvalidOperationException("User not found.");

            u.IsActive = isActive;
            await _db.SaveChangesAsync(ct);
        }

        public async Task SetUserRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, ct)
                ?? throw new InvalidOperationException("Role not found.");

            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
                ?? throw new InvalidOperationException("User not found.");

            u.RoleId = role.Id;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<ResetPasswordResponse> ResetPasswordAsync(Guid userId, CancellationToken ct = default)
        {
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
                ?? throw new InvalidOperationException("User not found.");

            string temp = GenerateTempPassword(8);
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temp);
            await _db.SaveChangesAsync(ct);

            return new ResetPasswordResponse(temp, DateTime.UtcNow);
        }

        private static string GenerateTempPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%&*?";
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            var arr = new char[length];
            for (int i = 0; i < length; i++) arr[i] = chars[bytes[i] % chars.Length];
            return new string(arr);
        }


        public async Task<PagedResult<PaymentRowDto>> GetPaymentsAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page <= 0) page = 1; if (pageSize is < 1 or > 200) pageSize = 25;

            var q = from p in _db.Payments.AsNoTracking()
                    join s in _db.Schools.AsNoTracking() on p.SchoolId equals s.Id into sj
                    from s in sj.DefaultIfEmpty()
                    orderby p.DateCreated descending
                    select new PaymentRowDto(
                        p.Id, p.SchoolId, s != null ? s.Name : "",
                        p.Currency, p.Subtotal, p.Vat, p.Total,
                        p.Status.ToString(), p.Provider, p.Reference, p.DateCreated
                    );

            var total = await q.CountAsync(ct);
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return new PagedResult<PaymentRowDto>(page, pageSize, total, items);
        }

        public async Task<PagedResult<SubscriptionRowDto>> GetSubscriptionsAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page <= 0) page = 1; if (pageSize is < 1 or > 200) pageSize = 25;

            var q = from s in _db.Subscriptions.AsNoTracking()
                    join sch in _db.Schools.AsNoTracking() on s.SchoolId equals sch.Id into sj
                    from sch in sj.DefaultIfEmpty()
                    orderby s.DateCreated descending
                    select new SubscriptionRowDto(
                        s.Id, s.SchoolId, sch != null ? sch.Name : "",
                        s.StudentsCovered, s.PricePerStudent,
                        s.StartsAt, s.EndsAt, s.Active, s.LastPaymentReference
                    );

            var total = await q.CountAsync(ct);
            var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

            return new PagedResult<SubscriptionRowDto>(page, pageSize, total, items);
        }

        public async Task<RevenueBreakdownDto> GetRevenueBreakdownAsync(CancellationToken ct = default)
        {
            const string key = "ga:revenue:v1";
            if (_cache.TryGetValue(key, out var boxed) && boxed is RevenueBreakdownDto cached)
                return cached;

            var total = await _db.Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.Total, ct) ?? 0m;

            var countPaid  = await _db.Payments.CountAsync(p => p.Status == PaymentStatus.Paid, ct);
            var subsActive = await _db.Subscriptions.CountAsync(s => s.Active, ct);

            var dto = new RevenueBreakdownDto(total, countPaid, subsActive, DateTime.UtcNow);
            _cache.Set(key, dto, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120) });
            return dto;
        }


        public async Task<byte[]> ExportCsvAsync(GlobalExportRequest req, CancellationToken ct = default)
        {
            string csv;

            if (string.Equals(req.Type, "payments", StringComparison.OrdinalIgnoreCase))
            {
                var data = await (from p in _db.Payments.AsNoTracking()
                                  join s in _db.Schools.AsNoTracking() on p.SchoolId equals s.Id into sj
                                  from s in sj.DefaultIfEmpty()
                                  orderby p.DateCreated descending
                                  select new
                                  {
                                      p.Id, School = s != null ? s.Name : "",
                                      p.Currency, p.Subtotal, p.Vat, p.Total,
                                      Status = p.Status.ToString(), p.Provider, p.Reference, p.DateCreated
                                  }).ToListAsync(ct);

                csv = "Id,School,Currency,Subtotal,Vat,Total,Status,Provider,Reference,DateCreated\n" +
                      string.Join("\n", data.Select(p =>
                        $"{p.Id},{Quote(p.School)},{p.Currency},{p.Subtotal},{p.Vat},{p.Total},{p.Status},{p.Provider},{p.Reference},{p.DateCreated:O}"));
            }
            else if (string.Equals(req.Type, "users", StringComparison.OrdinalIgnoreCase))
            {
                var data = await _db.Users.AsNoTracking()
                    .Include(u => u.Role)
                    .Include(u => u.School)
                    .OrderByDescending(u => u.DateCreated)
                    .ToListAsync(ct);

                csv = "Id,Name,Email,Role,School,IsActive,IsVerified,DateCreated,LastLogin\n" +
                      string.Join("\n", data.Select(u =>
                        $"{u.Id},{Quote(u.FullName)},{u.Email},{u.Role?.Name},{Quote(u.School?.Name)},{u.IsActive},{u.IsEmailVerified},{u.DateCreated:O},{u.LastLogin:O}"));
            }
            else
            {
                var data = await _db.Schools.AsNoTracking()
                    .OrderByDescending(s => s.DateCreated)
                    .ToListAsync(ct);

                csv = "Id,Name,Subdomain,Country,IsActive,DateCreated\n" +
                      string.Join("\n", data.Select(s =>
                        $"{s.Id},{Quote(s.Name)},{s.Subdomain},{s.Country},{s.IsActive},{s.DateCreated:O}"));
            }

            return System.Text.Encoding.UTF8.GetBytes(csv);

            static string Quote(string? s) => s is null ? "" : "\"" + s.Replace("\"", "\"\"") + "\"";
        }


        public async Task<SupportOverviewDto> GetSupportOverviewAsync(CancellationToken ct = default)
        {
            var since = DateTime.UtcNow.AddDays(-7);

            var messagesLast7d = await _db.MessageLogs.AsNoTracking()
                .CountAsync(m => m.SentAt >= since, ct);

            var messagesOpen = await _db.MessageLogs.AsNoTracking()
                .CountAsync(m => m.ToUserId != null && m.ReadAt == null, ct);

            var distinctSchoolsLast7d = await (
                from m in _db.MessageLogs.AsNoTracking()
                join c in _db.Classrooms.AsNoTracking() on m.ClassroomId equals c.Id
                where m.SentAt >= since
                select c.SchoolId
            ).Distinct().CountAsync(ct);

            return new SupportOverviewDto(messagesLast7d, messagesOpen, distinctSchoolsLast7d);
        }

        public async Task<PagedResult<SupportMessageDto>> GetSupportMessagesAsync(int page, int pageSize, CancellationToken ct = default)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 50;

            var baseQ = _db.MessageLogs.AsNoTracking();

            var total = await baseQ.CountAsync(ct);


            var q = from m in baseQ
                    join u in _db.Users.AsNoTracking() on m.FromUserId equals u.Id into uj
                    from u in uj.DefaultIfEmpty()
                    join c in _db.Classrooms.AsNoTracking() on m.ClassroomId equals c.Id into cj
                    from c in cj.DefaultIfEmpty()
                    orderby m.SentAt descending
                    select new SupportMessageDto(
                        m.Id,
                        m.FromUserId,
                        u != null ? u.Email : null,
                        m.Channel,
                        m.Body,
                        m.SentAt,
                        c != null ? (Guid?)c.SchoolId : null
                    );

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedResult<SupportMessageDto>(page, pageSize, total, items);
        }
    }
}
