using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

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

       
       
    }
}