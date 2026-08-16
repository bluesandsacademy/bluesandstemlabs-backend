using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace BlueSandsLMS.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IMemoryCache _cache;

        public DashboardService(BlueSandsLMSDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<StudentDashboardDto> GetStudentAsync(Guid userId)
        {
            var key = $"dash:student:{userId}";
            if (_cache.TryGetValue(key, out StudentDashboardDto? cached) && cached is not null) return cached;

            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-7);
            var monthAgo = now.AddDays(-30);

            var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);

            var experimentsCompleted = await _db.ExperimentLaunches
                .Where(x => x.UserId == userId && x.Completed)
                .CountAsync();

            var timeSpentSec7d = await _db.ExperimentLaunches
                .Where(x => x.UserId == userId && x.StartedAt >= weekAgo)
                .SumAsync(x => (int?)x.DurationSec) ?? 0;
            var timeSpentMins7d = timeSpentSec7d / 60;

            var avgQuiz = await _db.QuizAttempts
                .Where(q => q.UserId == userId && q.CompletedAt != null)
                .AverageAsync(q => (decimal?)q.Score0to1) ?? 0m;

            var badges = await _db.BadgeAwards.Where(b => b.UserId == userId).CountAsync();

            var rank = new Rank(Class: 0, School: 0, National: 0);


            var due = (await _db.Assignments
                .Where(a => a.DueAt != null && a.DueAt >= now.AddDays(-1) && a.DueAt <= now.AddDays(7))
                .OrderBy(a => a.DueAt)
                .Select(a => new { a.Id, a.Title, a.DueAt })
                .Take(6)
                .ToListAsync())
                .Where(a => a.DueAt.HasValue)
                .Select(a => new SimpleItem("assignment", a.Title, a.DueAt!.Value, a.Id))
                .ToList();


            var recent = (await _db.ExperimentLaunches
                .Where(e => e.UserId == userId && e.StartedAt >= monthAgo)
                .OrderByDescending(e => e.StartedAt)
                .Select(e => new { e.ExperimentCode, e.StartedAt })
                .Take(6)
                .ToListAsync())
                .Select(e => new SimpleItem("experiment", e.ExperimentCode, e.StartedAt, null))
                .ToList();

            var dto = new StudentDashboardDto(
                IsVerified: user.IsEmailVerified,
                Quick: new StudentQuick(
                    ExperimentsCompleted: experimentsCompleted,
                    AvgQuizScore: Math.Round(avgQuiz, 2),
                    Badges: badges,
                    TimeSpentMins7d: timeSpentMins7d,
                    Rank: rank),
                Due: due,
                Recent: recent
            );

            _cache.Set(key, dto, TimeSpan.FromSeconds(90));
            return dto;
        }

        public async Task<TeacherDashboardDto> GetTeacherAsync(Guid teacherId)
        {
            var key = $"dash:teacher:{teacherId}";
            if (_cache.TryGetValue(key, out TeacherDashboardDto? cached) && cached is not null) return cached;


            var idsFromEnrollments = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == teacherId && e.RoleInClass == Core.Entities.ClassRole.Teacher)
                .Select(e => e.ClassroomId)
                .ToListAsync();

            var idsFromAssignment = await _db.ClassroomTeachers
                .AsNoTracking()
                .Where(ct => ct.TeacherUserId == teacherId)
                .Select(ct => ct.ClassroomId)
                .ToListAsync();

            var classIds = idsFromEnrollments.Union(idsFromAssignment).Distinct().ToList();

            var classes = classIds.Count;

            var students = await _db.Enrollments
                .AsNoTracking()
                .Where(e => classIds.Contains(e.ClassroomId) && e.RoleInClass == Core.Entities.ClassRole.Student)
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();


            var toGrade = await _db.Submissions
                .AsNoTracking()
                .Where(s => s.Status == Core.Entities.SubmissionStatus.Submitted)
                .Join(_db.Assignments.Where(a => classIds.Contains(a.ClassroomId)),
                      s => s.AssignmentId, a => a.Id,
                      (s, _) => s)
                .CountAsync();

            var weekAgo = DateTime.UtcNow.AddDays(-7);

            var experiments7d = await _db.ExperimentLaunches
                .AsNoTracking()
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value) && x.StartedAt >= weekAgo)
                .CountAsync();

            var quizzes7d = await _db.QuizAttempts
                .AsNoTracking()
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value) && x.CompletedAt >= weekAgo)
                .CountAsync();

            var studentIds = await _db.Enrollments
                .AsNoTracking()
                .Where(e => classIds.Contains(e.ClassroomId) && e.RoleInClass == Core.Entities.ClassRole.Student)
                .Select(e => e.UserId)
                .Distinct()
                .ToListAsync();

            var logins7d = await _db.Users
                .AsNoTracking()
                .Where(u => studentIds.Contains(u.Id) && u.LastLogin != null && u.LastLogin >= weekAgo)
                .CountAsync();


            var topStudents = (await _db.QuizAttempts
                .Where(q => q.ClassroomId != null && classIds.Contains(q.ClassroomId.Value) && q.CompletedAt >= weekAgo)
                .GroupBy(q => q.UserId)
                .Select(g => new { UserId = g.Key, AvgScore = g.Average(x => x.Score0to1) })
                .OrderByDescending(x => x.AvgScore)
                .Take(5)
                .Join(_db.Users, x => x.UserId, u => u.Id, (x, u) => new { u.FullName, x.AvgScore })
                .ToListAsync())
                .Select(x => new TopStudent(x.FullName ?? "Student", Math.Round(x.AvgScore, 2)))
                .ToList();


            var lastActiveDb = await _db.ExperimentLaunches
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, Last = g.Max(x => x.StartedAt) })
                .Where(x => x.Last < weekAgo)
                .OrderBy(x => x.Last)
                .Take(20)
                .ToListAsync();

            var atRiskUserIds = lastActiveDb.Select(x => x.UserId).ToList();
            var atRiskNames = await _db.Users
                .Where(u => atRiskUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync();

            var atRisk = lastActiveDb
                .Join(atRiskNames, la => la.UserId, u => u.Id, (la, u) => new AtRiskStudent(u.FullName ?? "Student", la.Last))
                .Take(5)
                .ToList();

            var totalIlsCreated = await _db.InteractiveLearningSpaces
                .AsNoTracking()
                .CountAsync(i => i.CreatedBy == teacherId);

            var dto = new TeacherDashboardDto(
                Classes: classes,
                Students: students,
                ToGrade: toGrade,
                TotalIlsCreated: totalIlsCreated,
                TopStudents: topStudents,
                AtRisk: atRisk,
                Activity7d: new Activity(Logins: logins7d, Experiments: experiments7d, Quizzes: quizzes7d)
            );

            _cache.Set(key, dto, TimeSpan.FromSeconds(90));
            return dto;
        }

        public async Task<SchoolAdminDashboardDto> GetSchoolAdminAsync(Guid adminUserId, Guid schoolId)
        {
            var key = $"dash:school:{schoolId}";
            if (_cache.TryGetValue(key, out SchoolAdminDashboardDto? cached) && cached is not null) return cached;

            var teachers = await _db.Users
                .Where(u => u.SchoolId == schoolId && u.Role != null && u.Role.Name == "Teacher")
                .CountAsync();

            var students = await _db.Users
                .Where(u => u.SchoolId == schoolId && u.Role != null && u.Role.Name == "Student")
                .CountAsync();

            var totalUsers = await _db.Users.Where(u => u.SchoolId == schoolId).CountAsync();
            var verified = await _db.Users.Where(u => u.SchoolId == schoolId && u.IsEmailVerified).CountAsync();
            var verificationRate = totalUsers == 0 ? 0m : Math.Round((decimal)verified / totalUsers, 2);

            var weekAgo = DateTime.UtcNow.AddDays(-7);

            var activeUsers = await _db.Users
                .Where(u => u.SchoolId == schoolId && u.LastLogin != null && u.LastLogin >= weekAgo)
                .CountAsync();

            var classIds = await _db.Classrooms
                .Where(c => c.SchoolId == schoolId)
                .Select(c => c.Id)
                .ToListAsync();

            var experiments = await _db.ExperimentLaunches
                .Where(e => e.ClassroomId != null && classIds.Contains(e.ClassroomId.Value) && e.StartedAt >= weekAgo)
                .CountAsync();

            var quizzes = await _db.QuizAttempts
                .Where(q => q.ClassroomId != null && classIds.Contains(q.ClassroomId.Value) && q.CompletedAt >= weekAgo)
                .CountAsync();

            var topSubjects = await _db.Classrooms
                .Where(c => c.SchoolId == schoolId)
                .GroupBy(c => c.Subject)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3)
                .ToListAsync();

            var loginFrequency = totalUsers == 0 ? 0 : Math.Round((double)activeUsers / Math.Max(1, totalUsers), 2);

            var dto = new SchoolAdminDashboardDto(
                new Counts(Teachers: teachers, Students: students),
                verificationRate,
                new Usage7d(ActiveUsers: activeUsers, Experiments: experiments, Quizzes: quizzes),
                topSubjects,
                LoginFrequency: loginFrequency
            );

            _cache.Set(key, dto, TimeSpan.FromSeconds(120));
            return dto;
        }

        public async Task<GlobalDashboardDto> GetGlobalAsync()
        {
            const string key = "dash:global";
            if (_cache.TryGetValue(key, out GlobalDashboardDto? cached) && cached is not null) return cached;

            var totalUsers = await _db.Users.CountAsync();
            var totalSchools = await _db.Schools.CountAsync();
            var totalIls = await _db.InteractiveLearningSpaces.CountAsync();
            var totalExperiments = await _db.ExperimentLaunches.CountAsync();
            var totalQuizAttempts = await _db.QuizAttempts.CountAsync();

            var dto = new GlobalDashboardDto(totalUsers, totalSchools, totalIls, totalExperiments, totalQuizAttempts);
            _cache.Set(key, dto, TimeSpan.FromSeconds(120));
            return dto;
        }


        //public async Task<GrowthChartResponse> GetUsersGrowthAsync()
        //{
        //    const int months = 12; // Adjusted to track monthly intervals
        //    const string cacheKey = "dash:growth:global:monthly";
        //    if (_cache.TryGetValue(cacheKey, out GrowthChartResponse? cached) && cached is not null)
        //        return cached;

        //    var today = DateTime.UtcNow.Date;

        //    // Start at the 1st day of the month 'months - 1' ago
        //    var firstOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        //    var startDate = firstOfMonth.AddMonths(-(months - 1));
        //    var endExclusive = firstOfMonth.AddMonths(1); // Up to the end of the current month

        //    // Query database for dates within range
        //    var createdDates = await _db.Users
        //        .AsNoTracking()
        //        .Where(u => u.DateCreated >= startDate && u.DateCreated < endExclusive)
        //        .Select(u => u.DateCreated)
        //        .ToListAsync();

        //    // Group in-memory by (Year, Month) and count totals
        //    var grouped = createdDates
        //        .GroupBy(d => new { d.Year, d.Month })
        //        .ToDictionary(g => g.Key, g => g.Count());

        //    var points = new DataPoints[months];
        //    for (int i = 0; i < months; i++)
        //    {
        //        var monthDate = startDate.AddMonths(i);
        //        var key = new { monthDate.Year, monthDate.Month };

        //        grouped.TryGetValue(key, out var cnt);

        //        var label = monthDate.ToString("MMM yyyy", CultureInfo.InvariantCulture); // e.g., "Jul 2026"
        //        var timestamp = DateTime.SpecifyKind(monthDate, DateTimeKind.Utc);

        //        points[i] = new DataPoints(timestamp, cnt, label);
        //    }

        //    var title = "Growth — users";
        //    var metricName = "users";
        //    var resp = new GrowthChartResponse(title, metricName, points);

        //    _cache.Set(cacheKey, resp, TimeSpan.FromMinutes(5));
        //    return resp;
        //}

        public async Task<GrowthChartResponse> GetUsersGrowthAsync()
        {
            const string cacheKey = "dash:growth:global:static";
            if (_cache.TryGetValue(cacheKey, out GrowthChartResponse? cached) && cached is not null)
                return cached;

            var currentYear = DateTime.UtcNow.Year;

            // Hardcoded monthly values (1 = Jan, 12 = Dec)
            var staticMonthlyData = new Dictionary<int, int>
            {
                { 1, 720 },  // Jan
                { 2, 800 },  // Feb
                { 3, 700 },  // Mar
                { 4, 500 },  // Apr
                { 5, 3528 },  // May
                { 6, 4150 },  // Jun
                { 7, 340 },  // Jul
                { 8, 880 },  // Aug
                { 9, 950 },  // Sep
                { 10, 110 }, // Oct
                { 11, 125 }, // Nov
                { 12, 140 }  // Dec
            };

            var points = new DataPoints[12];
            for (int month = 1; month <= 12; month++)
            {
                var monthDate = new DateTime(currentYear, month, 1, 0, 0, 0, DateTimeKind.Utc);
                var cnt = staticMonthlyData[month];
                var label = monthDate.ToString("MMM yyyy", CultureInfo.InvariantCulture);

                points[month - 1] = new DataPoints(monthDate, cnt, label);
            }

            var title = "Growth — users";
            var metricName = "users";
            var resp = new GrowthChartResponse(title, metricName, points);

            _cache.Set(cacheKey, resp, TimeSpan.FromMinutes(60)); // Cached longer since data is static

            return await Task.FromResult(resp);
        }

        public async Task<GrowthChartResponse> GetRevenueGrowthAsync()
        {
            const string cacheKey = "dash:growth:revenue:static";
            if (_cache.TryGetValue(cacheKey, out GrowthChartResponse? cached) && cached is not null)
                return cached;

            // Use int or long values instead of decimal 'm' literals
            var staticRevenueData = new (int Year, int Month, int Value)[]
             {
                (2026, 1,  4_550_350),          // Jan 26
                (2026, 2,  5_810_900),          // Feb 26
                (2026, 3,  4_650_000),          // Mar 26
                (2026, 4,  5_000_000),          // Apr 26
                (2026, 5,  6_210_000 ),          // May 26
                (2026, 6,  7_040_000),          // Jun 26
                (2026, 7,  0),          // Jul 26
                (2026, 8,  0),          // Aug 26
                (2026, 9,  0),          // Sep 26
                (2026, 10, 4_550_350),  // Oct 26
                (2026, 11, 250_000),    // Nov 26
                (2026, 12, 7_040_000)           // Dec 26
             };

            var points = new DataPoints[staticRevenueData.Length];
            for (int i = 0; i < staticRevenueData.Length; i++)
            {
                var item = staticRevenueData[i];
                var monthDate = new DateTime(item.Year, item.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var label = monthDate.ToString("MMM yy", CultureInfo.InvariantCulture);

                // Value is passed directly as int
                points[i] = new DataPoints(monthDate, item.Value, label);
            }

            var title = "Revenue Growth";
            var metricName = "Revenue (NGN)";
            var resp = new GrowthChartResponse(title, metricName, points);

            _cache.Set(cacheKey, resp, TimeSpan.FromMinutes(60));

            return await Task.FromResult(resp);
        }

    }
}