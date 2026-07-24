using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlueSandsLMS.Common.Interfaces;
using System.Threading;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/exports")]
    [Authorize]
    public class ExportsController : ControllerBase
    {
        private readonly IExportService _exports;
        public ExportsController(IExportService exports) => _exports = exports;

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private string Role() => User.FindFirstValue(ClaimTypes.Role) ?? "";
        private Guid? SchoolIdClaim()
        {
            var s = User.FindFirstValue("SchoolId");
            return Guid.TryParse(s, out var id) ? id : (Guid?)null;
        }

        [HttpGet("gradebook")]
        public async Task<IActionResult> Gradebook([FromQuery] Guid classId)
        {

            var csv = await _exports.ExportGradebookCsvAsync(classId);
            return File(csv, "text/csv", $"gradebook-{classId}.csv");
        }

        [HttpGet("users")]
        public async Task<IActionResult> Users([FromQuery] Guid? schoolId = null)
        {
            var role = Role();
            var mySchoolId = SchoolIdClaim();

            if (role.Equals("SchoolAdmin", StringComparison.OrdinalIgnoreCase))
            {
                if (!schoolId.HasValue) schoolId = mySchoolId;
                if (schoolId != mySchoolId) return Forbid();
            }
            else if (!(role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("GlobalAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                return Forbid();
            }

            if (!schoolId.HasValue) return BadRequest("schoolId is required for this export.");

            var csv = await _exports.ExportUsersCsvAsync(schoolId.Value);
            return File(csv, "text/csv", $"users-{schoolId}.csv");
        }


        [HttpGet("engagement")]
        [Authorize(Roles = "Teacher,SchoolAdmin,GlobalAdmin")]
        public async Task<IActionResult> Engagement(
            [FromQuery] Guid? classroomId = null,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null,
            CancellationToken ct = default)
        {
            var from = fromUtc ?? DateTime.UtcNow.AddDays(-30);
            var to   = toUtc   ?? DateTime.UtcNow;
            var csv  = await _exports.ExportEngagementCsvAsync(CurrentUserId(), classroomId, from, to);
            return File(csv, "text/csv", $"engagement-{CurrentUserId()}-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
        }

        [HttpGet("activity")]
        public async Task<IActionResult> Activity([FromQuery] Guid? schoolId = null, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null)
        {
            var role = Role();
            var mySchoolId = SchoolIdClaim();

            if (role.Equals("SchoolAdmin", StringComparison.OrdinalIgnoreCase))
            {
                if (!schoolId.HasValue) schoolId = mySchoolId;
                if (schoolId != mySchoolId) return Forbid();
            }
            else if (!(role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("GlobalAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                return Forbid();
            }

            if (!schoolId.HasValue) return BadRequest("schoolId is required for this export.");

            var from = fromUtc ?? DateTime.UtcNow.AddDays(-30);
            var to = toUtc ?? DateTime.UtcNow;

            var csv = await _exports.ExportActivityCsvAsync(schoolId.Value, from, to);
            return File(csv, "text/csv", $"activity-{schoolId}-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
        }
    }
}
