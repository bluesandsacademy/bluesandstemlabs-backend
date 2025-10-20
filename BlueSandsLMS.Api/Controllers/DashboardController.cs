using BlueSandsLMS.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _svc;

        public DashboardController(IDashboardService svc) => _svc = svc;

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private string CurrentRole() => User.FindFirstValue(ClaimTypes.Role) ?? "";

        private Guid? CurrentSchoolId()
        {
            var s = User.FindFirstValue("SchoolId");
            return Guid.TryParse(s, out var id) ? id : (Guid?)null;
        }

        [HttpGet("student")]
        public async Task<IActionResult> Student()
        {
            var role = CurrentRole();
            if (!string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var me = CurrentUserId();
            return Ok(await _svc.GetStudentAsync(me));
        }

        [HttpGet("teacher")]
        public async Task<IActionResult> Teacher()
        {
            var role = CurrentRole();
            if (!string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var me = CurrentUserId();
            return Ok(await _svc.GetTeacherAsync(me));
        }

        [HttpGet("school-admin")]
        public async Task<IActionResult> SchoolAdmin()
        {
            var role = CurrentRole();
            if (!string.Equals(role, "SchoolAdmin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var me = CurrentUserId();
            var schoolId = CurrentSchoolId();
            if (schoolId is null) return BadRequest("SchoolId missing in token.");
            return Ok(await _svc.GetSchoolAdminAsync(me, schoolId.Value));
        }

        [HttpGet("global")]
        public async Task<IActionResult> Global()
        {
            var role = CurrentRole();
            var allowed = new[] { "GlobalAdmin", "Admin" };
            if (!allowed.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            return Ok(await _svc.GetGlobalAsync());
        }
    }
}
