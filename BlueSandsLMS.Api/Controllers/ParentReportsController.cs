using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlueSandsLMS.Common.Interfaces;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/parents")]
    [Authorize]
    public class ParentReportsController : ControllerBase
    {
        private readonly IParentReportService _svc;
        public ParentReportsController(IParentReportService svc) => _svc = svc;

        private string Role() => User.FindFirstValue(ClaimTypes.Role) ?? "";
        private Guid? SchoolIdClaim()
        {
            var s = User.FindFirstValue("SchoolId");
            return Guid.TryParse(s, out var id) ? id : (Guid?)null;
        }

        /// <summary>
        /// Triggers the monthly parent email report for a school.
        /// If month/year are omitted, previous month is used.
        /// </summary>
        [HttpPost("send-monthly")]
        public async Task<IActionResult> SendMonthly([FromQuery] Guid? schoolId = null, [FromQuery] int? year = null, [FromQuery] int? month = null)
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

            if (!schoolId.HasValue) return BadRequest("schoolId is required.");

            // Default: previous month (local policy)
            var now = DateTime.UtcNow;
            var prev = now.AddMonths(-1);
            int y = year ?? prev.Year;
            int m = month ?? prev.Month;

            var count = await _svc.SendMonthlyReportsAsync(schoolId.Value, y, m);
            var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m);
            return Ok(new { message = $"Dispatched {count} parent report emails for {monthName} {y}.", y, m, schoolId });
        }
    }
}
