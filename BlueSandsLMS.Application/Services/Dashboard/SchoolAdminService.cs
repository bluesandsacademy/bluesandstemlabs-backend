using System.Text;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.DTOs.Dashboard;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;  // BlueSandsLMSDbContext
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BlueSandsLMS.Core.Entities; // PaymentStatus, entities

// Interface aliases to avoid name collision
using ISchoolAdminAnalytics = BlueSandsLMS.Common.Interfaces.Dashboard.ISchoolAdminService;
using ISchoolAdminOps = BlueSandsLMS.Common.Interfaces.ISchoolAdminService;

namespace BlueSandsLMS.Application.Services.Dashboard
{
    internal static class Safe // tiny helpers to survive schema drift
    {
        // Try EF.Property in-db first, then return null if column is not there (caught at runtime if misspelled).
        public static IQueryable<TResult?> Col<TResult, TEntity>(this IQueryable<TEntity> q, string name)
            where TResult : struct
            => q.Select(e => EF.Property<TResult?>(e!, name));

        public static IQueryable<string?> ColStr<TEntity>(this IQueryable<TEntity> q, string name)
            => q.Select(e => EF.Property<string?>(e!, name));

        // Reflection helper for after ToListAsync
        public static T? Get<T>(object obj, params string[] names)
        {
            if (obj is null) return default;
            var t = obj.GetType();
            foreach (var n in names)
            {
                var p = t.GetProperty(n);
                if (p != null && p.PropertyType == typeof(T)) return (T?)p.GetValue(obj);
                if (p != null && typeof(T).IsAssignableFrom(p.PropertyType)) return (T?)p.GetValue(obj);
            }
            return default;
        }
    }

    /// <summary>
    /// Canonical School Admin service (dashboards + upserts),
    /// hardened against column/name differences found in your current schema.
    /// </summary>
    public sealed class SchoolAdminService : ISchoolAdminAnalytics, ISchoolAdminOps
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ICacheBustService _cacheBust;

        public SchoolAdminService(BlueSandsLMSDbContext db, IMemoryCache cache, ICacheBustService cacheBust)
        {
            _db = db;
            _cache = cache;
            _cacheBust = cacheBust;
        }

        // ===============  Analytics  ===============

        public async Task<SchoolOverviewDto> GetOverviewAsync(Guid schoolId, CancellationToken ct)
        {
            var key = $"sa:overview:{schoolId}";
            if (_cache.TryGetValue<SchoolOverviewDto>(key, out var cached) && cached is not null)
                return cached;

            var now = DateTimeOffset.UtcNow;
            var since30 = now.AddDays(-30).UtcDateTime;

            // role lookup (Teacher, Student)
            var roleIds = await _db.Roles
                .Where(r => r.Name == "Teacher" || r.Name == "Student")
                .Select(r => new { r.Id, r.Name })
                .ToListAsync(ct);
            var teacherRoleId = roleIds.FirstOrDefault(r => r.Name == "Teacher")?.Id;
            var studentRoleId = roleIds.FirstOrDefault(r => r.Name == "Student")?.Id;

            var activeTeachers = teacherRoleId == null
                ? 0
                : await _db.Users.CountAsync(u => u.SchoolId == schoolId && u.RoleId == teacherRoleId && u.IsActive, ct);

            var activeStudents = studentRoleId == null
                ? 0
                : await _db.Users.CountAsync(u => u.SchoolId == schoolId && u.RoleId == studentRoleId && u.IsActive, ct);

            // Experiments: scope by classroom
            var experiments = await _db.ExperimentLaunches
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      e => e.ClassroomId, c => c.Id, (e, c) => e)
                .CountAsync(ct);

            // Quizzes: distinct quiz keys from attempts
            var attemptsForDistinct = await _db.QuizAttempts
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      q => q.ClassroomId, c => c.Id, (q, c) => q)
                .Select(q => q)
                .ToListAsync(ct);

            var quizKeys = attemptsForDistinct
                .Select(q =>
                    // prefer QuizId / AssessmentId / QuizRef; else use q.Id as last resort
                    Safe.Get<Guid?>(q, "QuizId", "AssessmentId")?.ToString()
                    ?? Safe.Get<string>(q, "QuizRef", "QuizKey")
                    ?? Safe.Get<Guid>(q, "Id").ToString()
                )
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .Count();

            var newRegs30d = await _db.Users
                .CountAsync(u => u.SchoolId == schoolId && u.DateCreated >= since30, ct);

            var sub = await _db.Subscriptions
                .Where(s => s.SchoolId == schoolId && s.Active)
                .OrderByDescending(s => s.EndsAt)
                .FirstOrDefaultAsync(ct);

            var lastPayment = await _db.Payments
                .Where(p => p.SchoolId == schoolId && p.Status == PaymentStatus.Paid)
                .OrderByDescending(p => p.DateCreated)
                .FirstOrDefaultAsync(ct);

            // seats from StudentsCovered
            var seats = sub?.StudentsCovered ?? 0;
            var used = activeStudents;
            var percent = seats == 0 ? 0 : Math.Round(100.0 * used / seats, 1);

            var verified = await _db.Users.CountAsync(u => u.SchoolId == schoolId && u.IsEmailVerified, ct);
            var totalUsers = await _db.Users.CountAsync(u => u.SchoolId == schoolId, ct);
            var unverified = totalUsers - verified;
            var rate = totalUsers == 0 ? 0 : Math.Round(100.0 * verified / totalUsers, 1);

            // DAU by quiz completion in last 7d (CompletedAt might be nullable)
            var from7 = DateTime.UtcNow.Date.AddDays(-7);
            var dailyQuizUsers = await _db.QuizAttempts
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      q => q.ClassroomId, c => c.Id, (q, c) => q)
                .Where(q => EF.Property<DateTime?>(q, "CompletedAt") != null &&
                            EF.Property<DateTime?>(q, "CompletedAt")!.Value.Date >= from7)
                .GroupBy(q => EF.Property<DateTime?>(q, "CompletedAt")!.Value.Date)
                .Select(g => new { Date = DateOnly.FromDateTime(g.Key), Users = g.Select(x => EF.Property<Guid>(x, "UserId")).Distinct().Count() })
                .ToListAsync(ct);

            var last7 = Enumerable.Range(0, 7).Select(i => DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-i))).Reverse().ToList();
            var dau = last7.Select(d => dailyQuizUsers.FirstOrDefault(x => x.Date == d)?.Users ?? 0).ToList();

            // Compute last amount in kobo: prefer AmountKobo, else derive from Total
            long? lastAmountKobo = null;
            if (lastPayment is not null)
            {
                lastAmountKobo = (lastPayment.AmountKobo != 0)
                    ? lastPayment.AmountKobo
                    : (long?)(lastPayment.Total * 100m);
            }

            var dto = new SchoolOverviewDto(
                new TotalsDto(activeTeachers, activeStudents, experiments, quizKeys, newRegs30d),
                new SubscriptionCardDto(
                    sub?.EndsAt >= now,
                    /* Tier  */ "Unknown",
                    /* Seats */ sub?.StudentsCovered ?? 0,
                    sub?.EndsAt,
                    sub?.EndsAt is null ? 0 : Math.Max(0, (sub.EndsAt - now).Days)
                ),
                new BillingCardDto(
                    lastAmountKobo,
                    lastPayment?.DateCreated,
                    lastPayment?.Status.ToString() ?? "—",
                    lastPayment?.PromoCode
                ),
                new LicenseUtilizationDto(sub?.StudentsCovered ?? 0, used, percent),
                new VerificationDto(verified, unverified, rate),
                new Usage7dDto(dau)
            );

            _cache.Set(key, dto, TimeSpan.FromMinutes(5));
            return dto;
        }

        public async Task<TrendsDto> GetTrendsAsync(Guid schoolId, int days, CancellationToken ct)
{
    days = Math.Clamp(days <= 0 ? 30 : days, 7, 120);
    var since = DateTime.UtcNow.Date.AddDays(-days);

    // --- New Users (SQL → client map to DateOnly)
    var newUsersRaw = await _db.Users
        .Where(u => u.SchoolId == schoolId && u.DateCreated >= since)
        .GroupBy(u => u.DateCreated.Date)
        .Select(g => new { Date = g.Key, Count = g.Count() })
        .OrderBy(x => x.Date)
        .ToListAsync(ct);

    var newUsers = newUsersRaw
        .Select(x => new DateCount(DateOnly.FromDateTime(x.Date), x.Count))
        .ToList();

    // --- Daily Paid (pull minimal fields, aggregate client-side)
    var paidRows = await _db.Payments
        .Where(p => p.SchoolId == schoolId
                    && p.Status == Core.Entities.PaymentStatus.Paid
                    && p.DateCreated >= since)
        .Select(p => new { p.DateCreated, p.AmountKobo, p.Total })
        .ToListAsync(ct);

    var dailyPaid = paidRows
        .GroupBy(p => p.DateCreated.Date)
        .Select(g =>
        {
            long sum = 0;
            foreach (var x in g)
            {
                sum += (x.AmountKobo != 0) ? x.AmountKobo : (long)(x.Total * 100m);
            }
            return new DateAmount(DateOnly.FromDateTime(g.Key), sum);
        })
        .OrderBy(x => x.Date)
        .ToList();

    // --- Experiments (SQL → client map)
    var experimentsRaw = await _db.ExperimentLaunches
        .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
              e => e.ClassroomId, c => c.Id, (e, c) => e)
        .Where(e => e.StartedAt >= since)
        .GroupBy(e => e.StartedAt.Date)
        .Select(g => new { Date = g.Key, Count = g.Count() })
        .OrderBy(x => x.Date)
        .ToListAsync(ct);

    var experiments = experimentsRaw
        .Select(x => new DateCount(DateOnly.FromDateTime(x.Date), x.Count))
        .ToList();

    // --- Assignments (SQL → client map)
    var assignmentsRaw = await _db.Assignments
        .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
              a => a.ClassroomId, c => c.Id, (a, c) => a)
        .Where(a => a.CreatedAt >= since)
        .GroupBy(a => a.CreatedAt.Date)
        .Select(g => new { Date = g.Key, Count = g.Count() })
        .OrderBy(x => x.Date)
        .ToListAsync(ct);

    var assignments = assignmentsRaw
        .Select(x => new DateCount(DateOnly.FromDateTime(x.Date), x.Count))
        .ToList();

    return new TrendsDto(new TrendSeries(newUsers, dailyPaid, experiments, assignments));
}


        public async Task<PerformanceDto> GetPerformanceAsync(Guid schoolId, DateOnly? since, DateOnly? until, CancellationToken ct)
        {
            var from = (since ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))).ToDateTime(TimeOnly.MinValue);
            var to   = (until ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToDateTime(TimeOnly.MaxValue);

            // Pull attempts for this school and window
            var raw = await _db.QuizAttempts
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      q => q.ClassroomId, c => c.Id, (q, c) => q)
                .Where(q => EF.Property<DateTime?>(q, "CompletedAt") != null &&
                            EF.Property<DateTime?>(q, "CompletedAt")!.Value >= from &&
                            EF.Property<DateTime?>(q, "CompletedAt")!.Value <= to)
                .Select(q => q)
                .ToListAsync(ct);

            // Extract fields via reflection to survive different column names
            var shaped = raw.Select(q => new
            {
                Score   = Safe.Get<double?>(q, "Score", "Percentage", "ScorePercent") ?? 0.0,
                Passed  = Safe.Get<bool?>(q, "Passed", "IsPass") ?? false,
                Subject = Safe.Get<string>(q, "Subject", "Topic") ?? "Unknown",
                ClassId = Safe.Get<Guid>(q, "ClassroomId"),
                ClassName = Safe.Get<string>(Safe.Get<object?>(q, "Classroom") ?? new object(), "Name") ?? "(Class)"
            }).ToList();

            var overall = shaped.Count == 0 ? 0 : Math.Round(shaped.Average(x => x.Score), 2);
            var pass    = shaped.Count == 0 ? 0 : Math.Round(100.0 * shaped.Count(x => x.Passed) / shaped.Count, 1);

            var subjects = shaped
                .GroupBy(x => x.Subject)
                .Select(g => new SubjectScore(g.Key, Math.Round(g.Average(x => x.Score), 2), g.Count()))
                .OrderByDescending(s => s.Average)
                .ToList();

            var classes = shaped
                .GroupBy(x => new { x.ClassId, x.ClassName })
                .Select(g => new ClassScore(g.Key.ClassId, g.Key.ClassName, Math.Round(g.Average(x => x.Score), 2), g.Count()))
                .OrderByDescending(c => c.Average)
                .ToList();

            return new PerformanceDto(overall, pass, subjects, classes);
        }

        public async Task<TeacherActivityDto> GetTeacherActivityAsync(Guid schoolId, int days, CancellationToken ct)
        {
            days = Math.Clamp(days <= 0 ? 30 : days, 7, 120);
            var since = DateTime.UtcNow.AddDays(-days);

            // Assignments: count by CreatedById/TeacherId (fallback)
            var asgRaw = await _db.Assignments
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      a => a.ClassroomId, c => c.Id, (a, c) => a)
                .Where(a => a.CreatedAt >= since)
                .Select(a => a)
                .ToListAsync(ct);

            var teacherAsg = asgRaw
                .Select(a => new
                {
                    TeacherId = Safe.Get<Guid?>(a, "TeacherId", "CreatedById"),
                     TeacherName = Safe.Get<string>(Safe.Get<object?>(a, "Teacher") ?? new object(), "FullName")
                })
                .Where(x => x.TeacherId.HasValue)
                .GroupBy(x => new { Id = x.TeacherId!.Value, Name = x.TeacherName ?? "(Teacher)" })
                .Select(g => new TeacherAssignments(g.Key.Id, g.Key.Name, g.Count()))
                .OrderByDescending(x => x.Assignments)
                .ToList();

            // Grading turnaround via assignments join (already scoped)
            var graded = await _db.Submissions
                .Join(_db.Assignments.Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), a => a.ClassroomId, c => c.Id, (a, c) => a),
                      s => s.AssignmentId, a => a.Id, (s, a) => s)
                .Where(s => s.SubmittedAt != null && s.GradedAt != null && s.SubmittedAt >= since)
                .Select(s => new { s.SubmittedAt, s.GradedAt })
                .ToListAsync(ct);

            TimeSpan? avgTurnaround = null;
            if (graded.Count > 0)
            {
                var avgTicks = graded.Average(x => (x.GradedAt!.Value - x.SubmittedAt!.Value).Ticks);
                avgTurnaround = TimeSpan.FromTicks(Convert.ToInt64(avgTicks));
            }

            // Engagement score = assignments + “graded-by-teacher”
            var gradedByTeacher = await _db.Submissions
                .Join(_db.Assignments.Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), a => a.ClassroomId, c => c.Id, (a, c) => a),
                      s => s.AssignmentId, a => a.Id, (s, a) => new { s, a })
                .Where(x => x.s.GradedAt != null && x.s.GradedAt >= since)
                .Select(x => new
                {
                    TeacherId = Safe.Get<Guid?>(x.a, "TeacherId", "CreatedById"),
                    TeacherName = Safe.Get<string>(Safe.Get<object?>(x.a, "Teacher") ?? new object(), "FullName")
                })
                .ToListAsync(ct);

            var gradedCounts = gradedByTeacher
                .Where(x => x.TeacherId.HasValue)
                .GroupBy(x => new { Id = x.TeacherId!.Value, Name = x.TeacherName ?? "(Teacher)" })
                .Select(g => new { g.Key.Id, g.Key.Name, Count = g.Count() })
                .ToList();

            var m = new Dictionary<Guid, (string Name, int Score)>();
            foreach (var a in teacherAsg) m[a.TeacherId] = (a.TeacherName, a.Assignments);
            foreach (var g in gradedCounts)
            {
                var cur = m.TryGetValue(g.Id, out var t) ? t.Score : 0;
                m[g.Id] = (g.Name, cur + g.Count);
            }

            var engagement = m.Select(kv => new TeacherEngagement(kv.Key, kv.Value.Name, kv.Value.Score))
                              .OrderByDescending(x => x.Score).ToList();

            return new TeacherActivityDto(teacherAsg, avgTurnaround, engagement);
        }

        public async Task<ExperimentsCoursesDto> GetExperimentsAndCoursesAsync(Guid schoolId, int days, CancellationToken ct)
        {
            days = Math.Clamp(days <= 0 ? 30 : days, 7, 120);
            var since = DateTime.UtcNow.AddDays(-days);

            var totalExperiments = await _db.ExperimentLaunches
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      e => e.ClassroomId, c => c.Id, (e, c) => e)
                .CountAsync(ct);

            // class population from enrollments -> classroom
            var classPopulation = await _db.Enrollments
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      en => en.ClassroomId, c => c.Id, (en, c) => new { en, c })
                .GroupBy(x => new { x.c.Id, x.c.Name })
                .Select(g => new { ClassroomId = g.Key.Id, Name = g.Key.Name, Participants = g.Count() })
                .ToListAsync(ct);

            // completed per class: submissions -> assignment -> classroom
            var completedByClass = await _db.Submissions
                .Join(_db.Assignments.Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                                           a => a.ClassroomId, c => c.Id, (a, c) => a),
                      s => s.AssignmentId, a => a.Id, (s, a) => new { s, a.ClassroomId })
                .Where(x => x.s.SubmittedAt >= since)
                .GroupBy(x => x.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Completed = g.Select(x => x.s.StudentId).Distinct().Count() })
                .ToListAsync(ct);

            var completionRates = classPopulation.Select(c =>
            {
                var completed = completedByClass.FirstOrDefault(x => x.ClassroomId == c.ClassroomId)?.Completed ?? 0;
                var pct = c.Participants == 0 ? 0 : Math.Round(100.0 * completed / c.Participants, 1);
                return new ClassCompletionRate(c.ClassroomId, c.Name, pct, c.Participants);
            })
            .OrderByDescending(x => x.CompletionPercent)
            .ToList();

            // No materials telemetry in your current schema
            return new ExperimentsCoursesDto(totalExperiments, completionRates, 0.0, new List<ResourcePopularity>());
        }

        public Task<SystemMetricsDto> GetSystemMetricsAsync(Guid schoolId, int days, CancellationToken ct)
            => Task.FromResult(new SystemMetricsDto(new List<HourCount>(), new List<NameCount>(), new List<NameCount>(), new List<DateCount>()));

        public async Task<LeaderboardDto> GetLeaderboardAsync(Guid schoolId, int take, CancellationToken ct)
        {
            take = Math.Clamp(take <= 0 ? 10 : take, 1, 50);

            // Student id/name may not be on QuizAttempt navigation -> pull attempts, then join Users by UserId/StudentId
            var qa = await _db.QuizAttempts
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      q => q.ClassroomId, c => c.Id, (q, c) => q)
                .Select(q => new
                {
                    AttemptId = EF.Property<Guid>(q, "Id"),
                    UserId = EF.Property<Guid>(q, "UserId"),
                    Score = (double?)EF.Property<double?>(q, "Score")
                            ?? (double?)EF.Property<decimal?>(q, "Percentage")
                            ?? (double?)EF.Property<double?>(q, "ScorePercent")
                            ?? 0.0
                })
                .ToListAsync(ct);

            var studentAgg = qa
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, Score = g.Average(y => y.Score) })
                .OrderByDescending(x => x.Score)
                .Take(take)
                .ToList();

            var userIds = studentAgg.Select(x => x.UserId).ToList();
            var users = await _db.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.FullName }).ToListAsync(ct);

            var studentRanks = studentAgg
                .Select((x, i) => new StudentRank(x.UserId, users.FirstOrDefault(u => u.Id == x.UserId)?.FullName ?? "(Student)", Math.Round(x.Score, 2), i + 1))
                .ToList();

            // Teachers by assignment count (CreatedById/TeacherId fallback)
            var asg = await _db.Assignments
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId),
                      a => a.ClassroomId, c => c.Id, (a, c) => a)
                .Select(a => new
                {
                    TeacherId = (Guid?)EF.Property<Guid?>(a, "TeacherId") ?? EF.Property<Guid?>(a, "CreatedById")
                })
                .Where(x => x.TeacherId != null)
                .GroupBy(x => x.TeacherId!.Value)
                .Select(g => new { TeacherId = g.Key, Activities = g.Count() })
                .OrderByDescending(x => x.Activities)
                .Take(take)
                .ToListAsync(ct);

            var tIds = asg.Select(x => x.TeacherId).ToList();
            var tUsers = await _db.Users.Where(u => tIds.Contains(u.Id)).Select(u => new { u.Id, u.FullName }).ToListAsync(ct);

            var teacherRanks = asg
                .Select((t, i) => new TeacherRank(t.TeacherId, tUsers.FirstOrDefault(u => u.Id == t.TeacherId)?.FullName ?? "(Teacher)", t.Activities, i + 1))
                .ToList();

            return new LeaderboardDto(studentRanks, teacherRanks, /*RegionalCompare*/ null);
        }

        public async Task<BillingDto> GetBillingAsync(Guid schoolId, CancellationToken ct)
        {
            var sub = await _db.Subscriptions
                .Where(s => s.SchoolId == schoolId && s.Active)
                .OrderByDescending(s => s.EndsAt)
                .FirstOrDefaultAsync(ct);

            var payments = await _db.Payments.Where(p => p.SchoolId == schoolId)
                .OrderByDescending(p => p.DateCreated).Take(20)
                .Select(p => new PaymentRow(
                    p.Id,
                    p.AmountKobo != 0 ? p.AmountKobo : (long)(p.Total * 100m),
                    p.Status.ToString(),
                    p.DateCreated,
                    p.Reference,
                    p.PromoCode
                ))
                .ToListAsync(ct);

            return new BillingDto(
                new SubscriptionCardDto(
                    sub?.EndsAt >= DateTimeOffset.UtcNow,
                    "Standard",
                    sub?.StudentsCovered ?? 0,
                    sub?.EndsAt,
                    sub is null ? 0 : Math.Max(0, (sub.EndsAt - DateTimeOffset.UtcNow).Days)
                ),
                payments
            );
        }

        // ===============  Ops (legacy interface)  ===============

        public async Task<UpsertResultDto> UpsertTeacherAsync(Guid adminUserId, Guid schoolId, UpsertTeacherDto dto)
            => await UpsertUserForSchoolAsync(schoolId, dto.Email, dto.FullName, dto.Phone, dto.Country, "Teacher", CancellationToken.None);

        public async Task<IReadOnlyList<UpsertResultDto>> BulkUpsertTeachersAsync(Guid adminUserId, Guid schoolId, BulkUpsertTeachersDto dto)
        {
            var results = new List<UpsertResultDto>();
            foreach (var t in dto.Teachers.DistinctBy(x => x.Email.Trim().ToLowerInvariant()))
                results.Add(await UpsertTeacherAsync(adminUserId, schoolId, t));
            _cacheBust.InvalidateSchoolAdmin(schoolId);
            return results;
        }

        public async Task<UpsertResultDto> UpsertStudentAsync(Guid adminUserId, Guid schoolId, UpsertStudentDto dto)
            => await UpsertUserForSchoolAsync(schoolId, dto.Email, dto.FullName, dto.Phone, dto.Country, "Student", CancellationToken.None);

        public async Task<IReadOnlyList<UpsertResultDto>> BulkUpsertStudentsAsync(Guid adminUserId, Guid schoolId, BulkUpsertStudentsDto dto)
        {
            var results = new List<UpsertResultDto>();
            foreach (var s in dto.Students.DistinctBy(x => x.Email.Trim().ToLowerInvariant()))
                results.Add(await UpsertStudentAsync(adminUserId, schoolId, s));
            _cacheBust.InvalidateSchoolAdmin(schoolId);
            return results;
        }

        // Members required by your ISchoolAdminService (Ops)
        public async Task<Guid> CreateUserAsync(Guid schoolId, CreateUserRequest req, CancellationToken ct)
        {
            var res = await UpsertUserForSchoolAsync(schoolId, req.Email, req.FullName, null, null, req.Role, ct);
            return res.UserId;
        }

        public async Task AssignRoleAsync(Guid userId, string role, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                       ?? throw new InvalidOperationException("User not found");
            var oldSchoolId = user.SchoolId ?? Guid.Empty;

            var targetRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == role, ct)
                             ?? throw new Exception($"Role '{role}' not found.");
            user.RoleId = targetRole.Id;

            await _db.SaveChangesAsync(ct);
            if (oldSchoolId != Guid.Empty) _cacheBust.InvalidateSchoolAdmin(oldSchoolId);
        }

        // core helper
        private async Task<UpsertResultDto> UpsertUserForSchoolAsync(
            Guid schoolId, string email, string fullName, string? phone, string? country, string roleName, CancellationToken ct)
        {
            email = email.Trim();
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

            var targetRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct)
                             ?? throw new Exception($"Role '{roleName}' not found.");

            if (user == null)
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FullName = fullName,
                    Phone = phone ?? "",
                    Country = country ?? "",
                    RoleId = targetRole.Id,
                    SchoolId = schoolId,
                    DateCreated = DateTime.UtcNow,
                    IsActive = true
                };
                _db.Users.Add(newUser);
                await _db.SaveChangesAsync(ct);
                _cacheBust.InvalidateSchoolAdmin(schoolId);
                return new UpsertResultDto(email, "created", newUser.Id, roleName, schoolId);
            }

            if (user.SchoolId.HasValue && user.SchoolId.Value != schoolId)
                throw new Exception($"User '{email}' is already linked to another school.");

            if (!user.SchoolId.HasValue) user.SchoolId = schoolId;
            if (user.RoleId != targetRole.Id) user.RoleId = targetRole.Id;

            if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(fullName)) user.FullName = fullName;
            if (string.IsNullOrWhiteSpace(user.Phone) && !string.IsNullOrWhiteSpace(phone)) user.Phone = phone;
            if (string.IsNullOrWhiteSpace(user.Country) && !string.IsNullOrWhiteSpace(country)) user.Country = country;

            await _db.SaveChangesAsync(ct);
            _cacheBust.InvalidateSchoolAdmin(schoolId);
            return new UpsertResultDto(email, "updated", user.Id, roleName, schoolId);
        }

        // CSV bulk upload
        public async Task<BulkUploadResult> BulkUploadUsersCsvAsync(Guid schoolId, byte[] csvBytes, CancellationToken ct)
        {
            var errors = new List<string>();
            var created = 0; var updated = 0; var failed = 0;

            var text = Encoding.UTF8.GetString(csvBytes);
            var lines = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return new BulkUploadResult(0, 0, 0, new[] { "Empty CSV." });

            var header = lines[0].Split(',').Select(h => h.Trim()).ToArray();
            var map = header.Select((h, i) => (h: h.ToLowerInvariant(), i)).ToDictionary(x => x.h, x => x.i);
            int idx(string name) => map.TryGetValue(name.ToLowerInvariant(), out var i) ? i : -1;

            var iFull = idx("fullname");
            var iEmail = idx("email");
            var iRole = idx("role");
            var iPhone = idx("phone");
            var iCountry = idx("country");
            var iClass = idx("classroom");

            if (iFull < 0 || iEmail < 0 || iRole < 0)
                return new BulkUploadResult(0, 0, 0, new[] { "CSV must include FullName, Email, Role" });

            var seen = new HashSet<string>();

            for (int r = 1; r < lines.Length; r++)
            {
                var row = lines[r].Split(',');
                if (row.Length == 1 && string.IsNullOrWhiteSpace(row[0])) continue;

                try
                {
                    string full = iFull < row.Length ? row[iFull].Trim() : "";
                    string email = iEmail < row.Length ? row[iEmail].Trim().ToLowerInvariant() : "";
                    string role = iRole < row.Length ? row[iRole].Trim() : "";
                    string? phone = iPhone >= 0 && iPhone < row.Length ? row[iPhone].Trim() : null;
                    string? country = iCountry >= 0 && iCountry < row.Length ? row[iCountry].Trim() : null;
                    string? className = iClass >= 0 && iClass < row.Length ? row[iClass].Trim() : null;

                    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(full) || string.IsNullOrWhiteSpace(role))
                        throw new Exception($"Row {r + 1}: missing required fields.");

                    if (!seen.Add(email)) continue;

                    var res = await UpsertUserForSchoolAsync(schoolId, email, full, phone, country, role, ct);

                    if (!string.IsNullOrWhiteSpace(className))
                    {
                        var classroom = await _db.Classrooms.FirstOrDefaultAsync(c => c.SchoolId == schoolId && c.Name == className, ct);
                        if (classroom != null)
                        {
                            bool has = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classroom.Id && e.UserId == res.UserId, ct);
                            if (!has)
                            {
                                _db.Enrollments.Add(new Enrollment
                                {
                                    Id = Guid.NewGuid(),
                                    ClassroomId = classroom.Id,
                                    UserId = res.UserId
                                });
                                await _db.SaveChangesAsync(ct);
                            }
                        }
                    }

                    if (res.Action == "created") created++; else updated++;
                }
                catch (Exception ex)
                {
                    failed++; errors.Add(ex.Message);
                }
            }

            if (created + updated > 0) _cacheBust.InvalidateSchoolAdmin(schoolId);
            return new BulkUploadResult(created, updated, failed, errors);
        }
    }
}
