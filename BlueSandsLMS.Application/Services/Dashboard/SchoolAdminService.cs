using System.Text;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.DTOs.Dashboard;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Application.Services.Dashboard
{
    using ISchoolAdminAnalytics = BlueSandsLMS.Common.Interfaces.Dashboard.ISchoolAdminService;
    using ISchoolAdminOps = BlueSandsLMS.Common.Interfaces.ISchoolAdminService;

    using DashboardLeaderboardEntry = BlueSandsLMS.Common.DTOs.LeaderboardEntry;

    public sealed class SchoolAdminService : ISchoolAdminAnalytics, ISchoolAdminOps
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly ICacheBustService _cacheBust;

        public SchoolAdminService(BlueSandsLMSDbContext db, ICacheBustService cacheBust)
        {
            _db = db;
            _cacheBust = cacheBust;
        }

        public async Task<SchoolOverviewDto> GetOverviewAsync(Guid schoolId, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var teacherRoleId = await _db.Roles.Where(r => r.Name == "Teacher").Select(r => r.Id).FirstOrDefaultAsync(ct);
            var studentRoleId = await _db.Roles.Where(r => r.Name == "Student").Select(r => r.Id).FirstOrDefaultAsync(ct);

            var totalTeachers = teacherRoleId == Guid.Empty ? 0 :
                await _db.Users.CountAsync(u => u.SchoolId == schoolId && u.RoleId == teacherRoleId, ct);

            var totalStudents = studentRoleId == Guid.Empty ? 0 :
                await _db.Users.CountAsync(u => u.SchoolId == schoolId && u.RoleId == studentRoleId, ct);

            var activeClasses = await _db.Classrooms.CountAsync(c => c.SchoolId == schoolId, ct);

            var experimentsRunAllTime = await _db.ExperimentLaunches
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), e => e.ClassroomId, c => c.Id, (e, _) => e)
                .CountAsync(ct);

            var termSince = now.AddDays(-90).UtcDateTime;
            var experimentsRunThisTerm = await _db.ExperimentLaunches
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), e => e.ClassroomId, c => c.Id, (e, _) => e)
                .CountAsync(e => e.StartedAt >= termSince, ct);


            var avgScore = Math.Round(
                (double)(await _db.QuizAttempts.AsNoTracking()
                    .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), q => q.ClassroomId, c => c.Id, (q, _) => q)
                    .AverageAsync(q => (decimal?)q.Score0to1, ct) ?? 0m) * 100.0, 2);

            var avgCompletion = 0.0;

            var weekAgo7  = DateTime.UtcNow.AddDays(-7);
            var weekAgo30 = DateTime.UtcNow.AddDays(-30);

            var weeklyActiveUsers = await _db.QuizAttempts.AsNoTracking()
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), q => q.ClassroomId, c => c.Id, (q, _) => q)
                .Where(q => q.CompletedAt != null && q.CompletedAt >= weekAgo7)
                .Select(q => q.UserId)
                .Distinct()
                .CountAsync(ct);

            var monthlyActiveUsers = await _db.QuizAttempts.AsNoTracking()
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), q => q.ClassroomId, c => c.Id, (q, _) => q)
                .Where(q => q.CompletedAt != null && q.CompletedAt >= weekAgo30)
                .Select(q => q.UserId)
                .Distinct()
                .CountAsync(ct);

            var schoolName = await _db.Schools.Where(s => s.Id == schoolId).Select(s => s.Name).FirstOrDefaultAsync(ct) ?? "(School)";

            var totalIlsCreated = await _db.InteractiveLearningSpaces
                .Join(_db.Users.Where(u => u.SchoolId == schoolId), i => i.CreatedBy, u => u.Id, (i, _) => i)
                .CountAsync(ct);

            return new SchoolOverviewDto(
                schoolId, schoolName, totalStudents, totalTeachers, activeClasses,
                experimentsRunThisTerm, experimentsRunAllTime, avgCompletion, avgScore,
                weeklyActiveUsers, monthlyActiveUsers, totalIlsCreated
            );
        }

        public async Task<TrendsDto> GetTrendsAsync(Guid schoolId, int days, CancellationToken ct)
        {
            days = Math.Clamp(days <= 0 ? 30 : days, 7, 120);
            var since = DateTime.UtcNow.Date.AddDays(-days);

            var dailyUsers = await _db.QuizAttempts.AsNoTracking()
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), q => q.ClassroomId, c => c.Id, (q, _) => q)
                .Where(q => q.CompletedAt != null && q.CompletedAt.Value.Date >= since)
                .GroupBy(q => q.CompletedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Users = g.Select(x => x.UserId).Distinct().Count() })
                .OrderBy(x => x.Date).ToListAsync(ct);

            var activeUsers = dailyUsers
                .Select(x => new DataPoint(new DateTimeOffset(x.Date, TimeSpan.Zero), x.Users))
                .ToList();

            var ex = await _db.ExperimentLaunches
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), e => e.ClassroomId, c => c.Id, (e, _) => e)
                .Where(e => e.StartedAt >= since)
                .GroupBy(e => e.StartedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date).ToListAsync(ct);

            var experimentsRun = ex
                .Select(x => new DataPoint(new DateTimeOffset(x.Date, TimeSpan.Zero), x.Count))
                .ToList();

            var dailyScores = await _db.QuizAttempts.AsNoTracking()
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), q => q.ClassroomId, c => c.Id, (q, _) => q)
                .Where(q => q.CompletedAt != null && q.CompletedAt.Value.Date >= since)
                .Select(q => new
                {
                    D = q.CompletedAt!.Value.Date,
                    S = (double?)(q.Score0to1 * 100m)
                })
                .ToListAsync(ct);

            var avgScores = dailyScores
                .GroupBy(x => x.D)
                .Select(g =>
                {
                    var vals = g.Where(z => z.S.HasValue).Select(z => z.S!.Value).ToList();
                    var v = vals.Count == 0 ? 0.0 : vals.Average();
                    return new DataPoint(new DateTimeOffset(g.Key, TimeSpan.Zero), v);
                })
                .OrderBy(x => x.Ts).ToList();

            return new TrendsDto(activeUsers, experimentsRun, avgScores);
        }

        public async Task<PerformanceDto> GetPerformanceAsync(Guid schoolId, DateOnly? since, DateOnly? until, CancellationToken ct)
        {
            var from = (since ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))).ToDateTime(TimeOnly.MinValue);
            var to = (until ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToDateTime(TimeOnly.MaxValue);

            var scores = await _db.QuizAttempts
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), q => q.ClassroomId, c => c.Id, (q, _) => q)
                .Where(q => q.CompletedAt != null && q.CompletedAt >= from && q.CompletedAt <= to)
                .Select(q => (double)(q.Score0to1 * 100m))
                .ToListAsync(ct);

            var vals   = scores.OrderBy(x => x).ToList();
            var avg    = vals.Count == 0 ? 0.0 : Math.Round(vals.Average(), 2);
            var median = vals.Count == 0 ? 0.0 :
                (vals.Count % 2 == 1 ? vals[vals.Count / 2]
                                     : Math.Round((vals[vals.Count / 2 - 1] + vals[vals.Count / 2]) / 2.0, 2));

            var completedExperiments = await _db.ExperimentLaunches
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), e => e.ClassroomId, c => c.Id, (e, _) => e)
                .CountAsync(e => e.StartedAt >= from && e.StartedAt <= to, ct);

            var assigned = await _db.Assignments
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), a => a.ClassroomId, c => c.Id, (a, _) => a)
                .CountAsync(a => a.CreatedAt >= from && a.CreatedAt <= to, ct);

            var submitted = await _db.Submissions
                .Join(_db.Assignments.Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), a => a.ClassroomId, c => c.Id, (a, _) => a),
                      s => s.AssignmentId, a => a.Id, (s, a) => s)
                .CountAsync(s => s.SubmittedAt != null && s.SubmittedAt >= from && s.SubmittedAt <= to, ct);

            var completionRate = assigned == 0 ? 0.0 : Math.Round(100.0 * submitted / assigned, 1);

            return new PerformanceDto(avg, median, completedExperiments, completionRate);
        }

        public async Task<TeacherActivityDto> GetTeacherActivityAsync(Guid schoolId, int days, CancellationToken ct)
        {
            days = Math.Clamp(days <= 0 ? 30 : days, 7, 120);
            var since = DateTime.UtcNow.AddDays(-days);

            var perTeacher = await _db.Assignments
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), a => a.ClassroomId, c => c.Id, (a, _) => a)
                .Where(a => a.CreatedAt >= since)
                .GroupBy(a => new
                {
                    TeacherId = (Guid?)EF.Property<Guid?>(a, "TeacherId") ?? EF.Property<Guid?>(a, "CreatedById")
                })
                .Select(g => new { TeacherId = g.Key.TeacherId, ExperimentsAssigned = g.Count(), Classes = g.Select(x => EF.Property<Guid>(x, "ClassroomId")).Distinct().Count() })
                .Where(x => x.TeacherId != null)
                .OrderByDescending(x => x.ExperimentsAssigned)
                .FirstOrDefaultAsync(ct);

            if (perTeacher is null)
                return new TeacherActivityDto(Guid.Empty, "(Teacher)", null, 0, 0, 0, null);

            var tId = perTeacher.TeacherId!.Value;
            var t = await _db.Users.Where(u => u.Id == tId).Select(u => new { u.FullName, u.Email }).FirstOrDefaultAsync(ct);

            var studentsReached = await _db.Submissions
                .Join(_db.Assignments, s => s.AssignmentId, a => a.Id, (s, a) => new { s, a })
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), sa => sa.a.ClassroomId, c => c.Id, (sa, _) => sa)
                .Where(x => ((Guid?)EF.Property<Guid?>(x.a, "TeacherId") ?? EF.Property<Guid?>(x.a, "CreatedById")) == tId &&
                            x.s.SubmittedAt != null && x.s.SubmittedAt >= since)
                .Select(x => EF.Property<Guid>(x.s, "StudentId"))
                .Distinct()
                .CountAsync(ct);

            var lastGrade = await _db.Submissions
                .Join(_db.Assignments, s => s.AssignmentId, a => a.Id, (s, a) => new { s, a })
                .Where(x => ((Guid?)EF.Property<Guid?>(x.a, "TeacherId") ?? EF.Property<Guid?>(x.a, "CreatedById")) == tId &&
                            x.s.GradedAt != null)
                .MaxAsync(x => (DateTime?)x.s.GradedAt, ct);

            var lastAssign = await _db.Assignments
                .Where(a => ((Guid?)EF.Property<Guid?>(a, "TeacherId") ?? EF.Property<Guid?>(a, "CreatedById")) == tId)
                .MaxAsync(a => (DateTime?)a.CreatedAt, ct);

            DateTimeOffset? lastActive = null;
            if (lastGrade != null || lastAssign != null)
            {
                var max = new[] { lastGrade, lastAssign }.Where(d => d != null).Max();
                if (max != null) lastActive = new DateTimeOffset(max.Value);
            }

            return new TeacherActivityDto(
                tId,
                t?.FullName ?? "(Teacher)",
                t?.Email,
                perTeacher.Classes,
                perTeacher.ExperimentsAssigned,
                studentsReached,
                lastActive
            );
        }

        public async Task<ExperimentsCoursesDto> GetExperimentsAndCoursesAsync(Guid schoolId, int days, CancellationToken ct)
        {
            days = Math.Clamp(days <= 0 ? 30 : days, 7, 120);
            var since = DateTime.UtcNow.AddDays(-days);

            var ex = await _db.ExperimentLaunches
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), e => e.ClassroomId, c => c.Id, (e, _) => e)
                .Where(e => e.StartedAt >= since)
                .Select(e => new
                {
                    Title = (string?)EF.Property<string?>(e, "ExperimentName") ??
                            (string?)EF.Property<string?>(e, "Name") ??
                            "(Experiment)"
                })
                .ToListAsync(ct);

            var topExperiments = ex
                .GroupBy(x => x.Title ?? "(Experiment)")
                .Select(g => new ItemCount(Guid.Empty, g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            var topCourses = new List<ItemCount>();

            return new ExperimentsCoursesDto(topExperiments, topCourses);
        }

        public async Task<SystemMetricsDto> GetSystemMetricsAsync(Guid schoolId, int days, CancellationToken ct)
        {
            var totalSchools = await _db.Schools.CountAsync(ct);
            var totalTeachers = await _db.Users.CountAsync(u => u.Role!.Name == "Teacher", ct);
            var totalStudents = await _db.Users.CountAsync(u => u.Role!.Name == "Student", ct);
            var totalExperiments = await _db.ExperimentLaunches.CountAsync(ct);

            var mau = await _db.QuizAttempts
                .Where(q => EF.Property<DateTime?>(q, "CompletedAt") != null &&
                            EF.Property<DateTime?>(q, "CompletedAt")!.Value >= DateTime.UtcNow.AddDays(-30))
                .Select(q => EF.Property<Guid>(q, "UserId"))
                .Distinct()
                .CountAsync(ct);

            return new SystemMetricsDto(totalSchools, totalTeachers, totalStudents, totalExperiments, mau);
        }

        public async Task<LeaderboardDto> GetLeaderboardAsync(Guid schoolId, int take, CancellationToken ct)
        {
            take = Math.Clamp(take <= 0 ? 10 : take, 1, 50);

            var qa = await _db.QuizAttempts.AsNoTracking()
                .Join(_db.Classrooms.Where(c => c.SchoolId == schoolId), q => q.ClassroomId, c => c.Id, (q, _) => q)
                .Select(q => new
                {
                    q.UserId,
                    Score = (double)(q.Score0to1 * 100m)
                })
                .ToListAsync(ct);

            var top = qa.GroupBy(x => x.UserId)
                        .Select(g => new { UserId = g.Key, Points = g.Average(y => y.Score) })
                        .OrderByDescending(x => x.Points)
                        .Take(take)
                        .ToList();

            var ids = top.Select(t => t.UserId).ToList();
            var users = await _db.Users.Where(u => ids.Contains(u.Id))
                                       .Select(u => new { u.Id, u.FullName })
                                       .ToListAsync(ct);

            var entries = top.Select((x, i) =>

                new DashboardLeaderboardEntry(x.UserId, users.FirstOrDefault(u => u.Id == x.UserId)?.FullName ?? "(Student)", null,
                                     (int)Math.Round(x.Points), i + 1))
                .ToList();

            return new LeaderboardDto(entries, entries.Count, 1, entries.Count);
        }

        public async Task<BillingDto> GetBillingAsync(Guid schoolId, CancellationToken ct)
        {
            var sub = await _db.Subscriptions
                .Where(s => s.SchoolId == schoolId && s.Active)
                .OrderByDescending(s => s.EndsAt)
                .FirstOrDefaultAsync(ct);

            var seatsUsed = await _db.Users.CountAsync(u => u.SchoolId == schoolId && u.Role!.Name == "Student" && u.IsActive, ct);


            var isTrial = sub?.LastPaymentReference == "TRIAL";
            var now      = DateTime.UtcNow;
            var daysLeft = sub?.EndsAt != null
                ? Math.Max(0, (int)Math.Ceiling((sub.EndsAt - now).TotalDays))
                : 0;

            var planName = isTrial ? "Free Trial"
                         : sub != null ? "Standard"
                         : "None";

            var planStatus = isTrial
                ? (sub!.Active && sub.EndsAt >= now
                    ? $"Trial — {daysLeft} day{(daysLeft == 1 ? "" : "s")} remaining"
                    : "Trial expired")
                : ((sub?.Active ?? false) ? "Active" : "Inactive");

            var subscription = new SubscriptionCardDto(
                planName,
                planStatus,
                sub?.StartsAt,
                sub?.EndsAt,
                sub?.StudentsCovered ?? 0,
                seatsUsed,
                isTrial ? "Trial" : "Manual"
            );


            var payments = await _db.Payments.AsNoTracking()
                .Where(p => p.SchoolId == schoolId)
                .OrderByDescending(p => p.DateCreated).Take(20)
                .Select(p => new PaymentRow(
                    p.Id,
                    p.AmountKobo != 0 ? p.AmountKobo : (long)(p.Total * 100m),
                    p.Status.ToString(),
                    p.DateCreated,
                    p.Reference,
                    p.PromoCode
                )
                {
                    Currency = p.Currency,
                    Method   = p.Provider
                })
                .ToListAsync(ct);


            var tiers = await _db.PricingTiers.AsNoTracking()
                .OrderBy(t => t.MinStudents)
                .Select(t => new TierSummaryDto
                {
                    Id               = t.Id,
                    TierName         = t.TierName,
                    MinStudents      = t.MinStudents,
                    MaxStudents      = t.MaxStudents,
                    PricePerStudent  = t.PricePerStudent,
                    IsMatch          = sub != null &&
                                       sub.StudentsCovered >= t.MinStudents &&
                                       sub.StudentsCovered <= t.MaxStudents
                })
                .ToListAsync(ct);

            return new BillingDto(subscription, payments) { AvailableTiers = tiers };
        }



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

        public async Task<Guid> CreateUserAsync(Guid schoolId, CreateUserRequest req, CancellationToken ct)
        {
            var res = await UpsertUserForSchoolAsync(schoolId, req.Email, req.FullName, null, null, req.Role, ct);
            return res.UserId;
        }

        public async Task AssignRoleAsync(Guid userId, string role, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                       ?? throw new InvalidOperationException("User not found");

            var targetRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == role, ct)
                             ?? throw new Exception($"Role '{role}' not found.");

            user.RoleId = targetRole.Id;
            await _db.SaveChangesAsync(ct);

            if (user.SchoolId is Guid sid) _cacheBust.InvalidateSchoolAdmin(sid);
        }

        private async Task<UpsertResultDto> UpsertUserForSchoolAsync(
            Guid schoolId, string email, string fullName, string? phone, string? country, string roleName, CancellationToken ct)
        {
            email = email.Trim();
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

            var targetRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct)
                             ?? throw new Exception($"Role '{roleName}' not found.");

            if (user == null)
            {

var newUser = new Core.Entities.User
{
    Id = Guid.NewGuid(),
    Email = email,
    FullName = fullName,
    Phone = phone ?? "",
    Country = country ?? "",
    RoleId = targetRole.Id,
    SchoolId = schoolId,
    DateCreated = DateTime.UtcNow,
    IsActive = true,
    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
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
                                _db.Enrollments.Add(new Core.Entities.Enrollment
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