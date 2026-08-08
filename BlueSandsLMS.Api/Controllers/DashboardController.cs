using BlueSandsLMS.Api.Services;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _svc;
        private readonly ICurrentUser _currentUser;

        public DashboardController(IDashboardService svc, ICurrentUser currentUser)
        {
            _svc = svc;
            _currentUser = currentUser;
        }

        private Guid CurrentUserId() => _currentUser.GetUserId();

        private Guid? CurrentSchoolId() => _currentUser.SchoolId;

        [HttpGet("student")]
        public async Task<IActionResult> Student()
        {
            if (!_currentUser.IsInRole("Student"))
                return Forbid();

            var me = CurrentUserId();
            return Ok(await _svc.GetStudentAsync(me));
        }

        [HttpGet("teacher")]
        public async Task<IActionResult> Teacher()
        {
            if (!_currentUser.IsInRole("Teacher"))
                return Forbid();

            var me = CurrentUserId();
            return Ok(await _svc.GetTeacherAsync(me));
        }

        [HttpGet("school-admin")]
        public async Task<IActionResult> SchoolAdmin()
        {
            if (!_currentUser.IsInRole("SchoolAdmin"))
                return Forbid();

            var me = CurrentUserId();
            var schoolId = CurrentSchoolId();
            if (schoolId is null) return BadRequest("SchoolId missing in token.");
            return Ok(await _svc.GetSchoolAdminAsync(me, schoolId.Value));
        }

        [HttpGet("global")]
        public async Task<IActionResult> Global()
        {
            if (!(_currentUser.IsInRole("GlobalAdmin") || _currentUser.IsInRole("Admin")))
                return Forbid();

            return Ok(await _svc.GetGlobalAsync());
        }

    }
}