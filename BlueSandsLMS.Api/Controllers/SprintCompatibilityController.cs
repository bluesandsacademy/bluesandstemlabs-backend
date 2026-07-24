using System.Security.Claims;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Common.Interfaces.Teacher;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ISchoolAdminAnalytics = BlueSandsLMS.Common.Interfaces.Dashboard.ISchoolAdminService;
using ISchoolAdminOps = BlueSandsLMS.Common.Interfaces.ISchoolAdminService;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    public sealed class SprintCompatibilityController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IDashboardService _dashboard;
        private readonly IStudentDashboardService _studentDashboard;
        private readonly ITeacherAnalyticsService _teacherAnalytics;
        private readonly ISchoolAdminAnalytics _schoolAnalytics;
        private readonly ISchoolAdminOps _schoolOps;

        public SprintCompatibilityController(
            BlueSandsLMSDbContext db,
            IDashboardService dashboard,
            IStudentDashboardService studentDashboard,
            ITeacherAnalyticsService teacherAnalytics,
            ISchoolAdminAnalytics schoolAnalytics,
            ISchoolAdminOps schoolOps)
        {
            _db = db;
            _dashboard = dashboard;
            _studentDashboard = studentDashboard;
            _teacherAnalytics = teacherAnalytics;
            _schoolAnalytics = schoolAnalytics;
            _schoolOps = schoolOps;
        }

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private async Task<Guid> CurrentSchoolIdAsync(CancellationToken ct)
        {
            var claim = User.FindFirstValue("SchoolId");
            if (Guid.TryParse(claim, out var claimSchoolId) && claimSchoolId != Guid.Empty)
                return claimSchoolId;

            var userId = CurrentUserId();
            var dbSchoolId = await _db.Users.AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => x.SchoolId)
                .FirstOrDefaultAsync(ct);

            if (dbSchoolId is null || dbSchoolId == Guid.Empty)
                throw new UnauthorizedAccessException("SchoolId missing in token.");

            return dbSchoolId.Value;
        }

        [HttpGet("api/teacher/dashboard")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> TeacherDashboard()
            => Ok(await _dashboard.GetTeacherAsync(CurrentUserId()));

        [HttpGet("api/student/v1/dashboard")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentDashboard(CancellationToken ct)
            => Ok(await _studentDashboard.GetOverviewAsync(CurrentUserId(), ct));

        [HttpGet("api/school-admin/analytics")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> SchoolAnalytics(CancellationToken ct)
        {
            var schoolId = await CurrentSchoolIdAsync(ct);
            var overview = await _schoolAnalytics.GetOverviewAsync(schoolId, ct);
            var trends = await _schoolAnalytics.GetTrendsAsync(schoolId, 30, ct);
            return Ok(new { overview, trends });
        }

        [HttpGet("api/school-admin/billing")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> SchoolBilling(CancellationToken ct)
            => Ok(await _schoolAnalytics.GetBillingAsync(await CurrentSchoolIdAsync(ct), ct));

        [HttpGet("api/school-admin/billing/plans")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> SchoolBillingPlans(CancellationToken ct)
        {
            var plans = await _db.PricingTiers.AsNoTracking()
                .OrderBy(x => x.MinStudents)
                .Select(x => new { x.Id, x.TierName, x.MinStudents, x.MaxStudents, x.PricePerStudent })
                .ToListAsync(ct);

            return Ok(plans);
        }

        [HttpGet("api/teacher/performance-metrics")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> TeacherPerformanceMetrics(CancellationToken ct)
        {
            var to = DateTime.UtcNow;
            var from = to.AddDays(-30);
            var metrics = await _teacherAnalytics.PerformanceAsync(CurrentUserId(), null, null, from, to, ct);
            return Ok(metrics);
        }

        [HttpPost("api/school-admin/teachers")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> CreateTeacher([FromBody] UpsertTeacherDto dto, CancellationToken ct)
            => Ok(await _schoolOps.UpsertTeacherAsync(CurrentUserId(), await CurrentSchoolIdAsync(ct), dto));

        [HttpGet("api/school-admin/teachers")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> ListTeachers(CancellationToken ct)
        {
            var schoolId = await CurrentSchoolIdAsync(ct);
            var teacherRoleId = await _db.Roles.AsNoTracking()
                .Where(x => x.Name == "Teacher")
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            var teachers = await _db.Users.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.RoleId == teacherRoleId && x.IsActive)
                .OrderBy(x => x.FullName)
                .Select(x => new { x.Id, x.FullName, x.Email, x.Phone, x.Country, x.DateCreated, x.IsEmailVerified })
                .ToListAsync(ct);

            return Ok(teachers);
        }

        [HttpGet("api/school-admin/roles")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> Roles(CancellationToken ct)
        {
            var roles = await _db.Roles.AsNoTracking()
                .Where(x => x.Name == "Teacher" || x.Name == "Student" || x.Name == "SchoolAdmin")
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);

            return Ok(roles);
        }

        public sealed record AssignSchoolRoleRequest(Guid UserId, string Role);

        [HttpPost("api/school-admin/roles/assign")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> AssignRole([FromBody] AssignSchoolRoleRequest req, CancellationToken ct)
        {
            var schoolId = await CurrentSchoolIdAsync(ct);
            var belongsToSchool = await _db.Users.AsNoTracking()
                .AnyAsync(x => x.Id == req.UserId && x.SchoolId == schoolId, ct);

            if (!belongsToSchool)
                return NotFound(new { message = "User not found in this school." });

            await _schoolOps.AssignRoleAsync(req.UserId, req.Role, ct);
            return Ok(new { message = "Role assigned." });
        }

        public sealed record AddClassStudentRequest(string Email);

        [HttpPost("api/classes/{classId:guid}/students")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> AddClassStudent(Guid classId, [FromBody] AddClassStudentRequest req, CancellationToken ct)
        {
            var userId = await _db.Users.AsNoTracking()
                .Where(x => x.Email == req.Email && x.IsActive)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (userId == Guid.Empty)
                return NotFound(new { message = "Student not found." });

            var existing = await _db.Enrollments
                .FirstOrDefaultAsync(x => x.ClassroomId == classId && x.UserId == userId, ct);

            if (existing == null)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    Id = Guid.NewGuid(),
                    ClassroomId = classId,
                    UserId = userId,
                    RoleInClass = ClassRole.Student,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.RoleInClass = ClassRole.Student;
            }

            await _db.SaveChangesAsync(ct);
            return Ok(new { studentId = userId, classId });
        }

        [HttpGet("api/classes/{classId:guid}/students")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ListClassStudents(Guid classId, CancellationToken ct)
        {
            var students = await _db.Enrollments.AsNoTracking()
                .Where(x => x.ClassroomId == classId && x.RoleInClass == ClassRole.Student)
                .Join(_db.Users.AsNoTracking(), e => e.UserId, u => u.Id, (e, u) => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    e.CreatedAt
                })
                .OrderBy(x => x.FullName)
                .ToListAsync(ct);

            return Ok(students);
        }

        [HttpGet("api/student/v1/assessments")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentAssessments(CancellationToken ct)
        {
            var userId = CurrentUserId();
            var quizAttempts = await _db.QuizAttempts.AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CompletedAt ?? x.StartedAt)
                .Select(x => new
                {
                    id = x.Id,
                    type = "Quiz",
                    title = x.QuizCode,
                    scorePercent = Math.Round((double)x.Score0to1 * 100.0, 2),
                    x.Passed,
                    completedAt = x.CompletedAt ?? x.StartedAt
                })
                .Take(25)
                .ToListAsync(ct);

            return Ok(quizAttempts);
        }

        [HttpGet("api/student/v1/experiments/launch/{simId:guid}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> LaunchSimulation(Guid simId, CancellationToken ct)
        {
            var sim = await _db.PhETSimulations.AsNoTracking()
                .Where(x => x.Id == simId && x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.IsFree,
                    Url = x.RunnableResource ?? x.SimulationUrl ?? x.SimPage
                })
                .FirstOrDefaultAsync(ct);

            if (sim == null)
                return NotFound(new { message = "Simulation not found." });

            var userId = CurrentUserId();
            var user = await _db.Users.AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new { x.Id, x.SchoolId })
                .FirstOrDefaultAsync(ct);

            if (user == null)
                return Unauthorized();

            var now = DateTime.UtcNow;
            var hasAccess = sim.IsFree || await _db.Subscriptions.AsNoTracking()
                .AnyAsync(x =>
                    x.Active &&
                    x.StartsAt <= now &&
                    x.EndsAt >= now &&
                    ((user.SchoolId != null && x.SchoolId == user.SchoolId.Value) || x.UserId == userId),
                    ct);

            if (!hasAccess)
                return Forbid();

            return Ok(new { sim.Id, sim.Title, launchUrl = sim.Url });
        }
    }
}
