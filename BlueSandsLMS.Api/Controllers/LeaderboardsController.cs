using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/leaderboards")]
    [Authorize]
    public class LeaderboardsController : ControllerBase
    {
        private readonly ILeaderboardService _svc;
        private readonly BlueSandsLMSDbContext _db;
        public LeaderboardsController(ILeaderboardService svc, BlueSandsLMSDbContext db)
        {
            _svc = svc; _db = db;
        }

        private string Role() => User.FindFirstValue(ClaimTypes.Role) ?? "";
        private Guid? SchoolIdClaim()
        {
            var s = User.FindFirstValue("SchoolId");
            return Guid.TryParse(s, out var id) ? id : (Guid?)null;
        }
        private Guid UserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        [HttpGet("class/{classId:guid}")]
        public async Task<IActionResult> Class(Guid classId, [FromQuery] string metric = "quiz", [FromQuery] int top = 50)
        {
            var me = UserId();
            var role = Role();


            var myEnrollment = await _db.Enrollments
                .AnyAsync(e => e.ClassroomId == classId && e.UserId == me);

            var mySchoolId = SchoolIdClaim();

            var classObj = await _db.Classrooms.FirstOrDefaultAsync(c => c.Id == classId);
            if (classObj == null) return NotFound("Class not found.");

            var schoolAdminOk = mySchoolId.HasValue && classObj.SchoolId == mySchoolId.Value &&
                                (role.Equals("SchoolAdmin", StringComparison.OrdinalIgnoreCase) || role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("GlobalAdmin", StringComparison.OrdinalIgnoreCase));

            if (!myEnrollment && !schoolAdminOk)
                return Forbid();

            return Ok(await _svc.GetClassAsync(classId, metric, top));
        }

        [HttpGet("school/{schoolId:guid}")]
        public async Task<IActionResult> School(Guid schoolId, [FromQuery] string metric = "quiz", [FromQuery] int top = 50)
        {
            var role = Role();
            var mySchoolId = SchoolIdClaim();

            if (!(role.Equals("SchoolAdmin", StringComparison.OrdinalIgnoreCase) || role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("GlobalAdmin", StringComparison.OrdinalIgnoreCase)))
                return Forbid();

            if (role.Equals("SchoolAdmin", StringComparison.OrdinalIgnoreCase) && mySchoolId != schoolId)
                return Forbid();

            return Ok(await _svc.GetSchoolAsync(schoolId, metric, top));
        }

        [HttpGet("global")]
        public async Task<IActionResult> Global([FromQuery] string metric = "quiz", [FromQuery] int top = 50)
        {
            var role = Role();
            if (!(role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("GlobalAdmin", StringComparison.OrdinalIgnoreCase)))
                return Forbid();

            return Ok(await _svc.GetGlobalAsync(metric, top));
        }
    }
}
