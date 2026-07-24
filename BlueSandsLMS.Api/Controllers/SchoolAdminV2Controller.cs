using System.Security.Claims;
using BlueSandsLMS.Common.DTOs.Dashboard;
using ISchoolAdminAnalytics = BlueSandsLMS.Common.Interfaces.Dashboard.ISchoolAdminService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/school-admin/v2")]
    [Authorize(Roles = "SchoolAdmin")]
    public sealed class SchoolAdminV2Controller : ControllerBase
    {
        private readonly ISchoolAdminAnalytics _svc;
        private readonly BlueSandsLMSDbContext _db;
        public SchoolAdminV2Controller(ISchoolAdminAnalytics svc, BlueSandsLMSDbContext db)
        {
            _svc = svc; _db = db;
        }

        private async Task<Guid> RequireSchoolIdAsync(CancellationToken ct)
        {
            var uid = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid)) throw new UnauthorizedAccessException("User id missing");
            var userId = Guid.Parse(uid);

            var schoolId = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(ct);

            if (schoolId == null || schoolId == Guid.Empty) throw new UnauthorizedAccessException("SchoolId missing");
            return schoolId.Value;
        }

        [HttpGet("overview")]
        public async Task<SchoolOverviewDto> Overview(CancellationToken ct)
            => await _svc.GetOverviewAsync(await RequireSchoolIdAsync(ct), ct);

        [HttpGet("trends")]
        public async Task<TrendsDto> Trends([FromQuery] int days = 30, CancellationToken ct = default)
            => await _svc.GetTrendsAsync(await RequireSchoolIdAsync(ct), days, ct);

        [HttpGet("performance")]
        public async Task<PerformanceDto> Performance([FromQuery] DateOnly? since, [FromQuery] DateOnly? until, CancellationToken ct = default)
            => await _svc.GetPerformanceAsync(await RequireSchoolIdAsync(ct), since, until, ct);

        [HttpGet("teacher-activity")]
        public async Task<TeacherActivityDto> TeacherActivity([FromQuery] int days = 30, CancellationToken ct = default)
            => await _svc.GetTeacherActivityAsync(await RequireSchoolIdAsync(ct), days, ct);

        [HttpGet("experiments-and-courses")]
        public async Task<ExperimentsCoursesDto> ExperimentsAndCourses([FromQuery] int days = 30, CancellationToken ct = default)
            => await _svc.GetExperimentsAndCoursesAsync(await RequireSchoolIdAsync(ct), days, ct);

        [HttpGet("system-metrics")]
        public async Task<SystemMetricsDto> SystemMetrics([FromQuery] int days = 30, CancellationToken ct = default)
            => await _svc.GetSystemMetricsAsync(await RequireSchoolIdAsync(ct), days, ct);

        [HttpGet("leaderboard")]
        public async Task<LeaderboardDto> Leaderboard([FromQuery] int take = 10, CancellationToken ct = default)
            => await _svc.GetLeaderboardAsync(await RequireSchoolIdAsync(ct), take, ct);

        [HttpGet("billing")]
        public async Task<BillingDto> Billing(CancellationToken ct)
            => await _svc.GetBillingAsync(await RequireSchoolIdAsync(ct), ct);

        [HttpGet("profile")]
        public async Task<IActionResult> Profile(CancellationToken ct)
        {
            var uid = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid) || !Guid.TryParse(uid, out var userId))
                return Unauthorized();

            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null) return Unauthorized();

            string? schoolName = null;
            string schoolCurrency = "NGN";
            int totalStudents = 0;
            string schoolCountry = string.Empty;

            int? estimatedScienceStudentCount = null;
            int? disabledStudentCount         = null;

            if (user.SchoolId.HasValue)
            {
                var school = await _db.Schools
                    .AsNoTracking()
                    .Where(s => s.Id == user.SchoolId.Value)
                    .Select(s => new
                    {
                        s.Name, s.Currency, s.Country, s.TotalStudents,
                        s.EstimatedScienceStudentCount, s.DisabledStudentCount
                    })
                    .FirstOrDefaultAsync(ct);

                schoolName                   = school?.Name;
                schoolCurrency               = school?.Currency ?? "NGN";
                totalStudents                = school?.TotalStudents ?? 0;
                schoolCountry                = school?.Country ?? string.Empty;
                estimatedScienceStudentCount = school?.EstimatedScienceStudentCount;
                disabledStudentCount         = school?.DisabledStudentCount;
            }

            return Ok(new
            {
                userId         = user.Id,
                fullName       = user.FullName,
                email          = user.Email,
                phone          = user.Phone,
                country        = user.Country ?? schoolCountry,
                role           = user.Role?.Name ?? "SchoolAdmin",
                schoolId       = user.SchoolId,
                schoolName,
                schoolCurrency,
                totalStudents,
                estimatedScienceStudentCount,
                disabledStudentCount,
                isEmailVerified = user.IsEmailVerified
            });
        }
    }
}
