using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/leaderboard")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1LeaderboardController : ControllerBase
    {
        private readonly IExtendedLeaderboardService _svc;
        public GlobalAdminV1LeaderboardController(IExtendedLeaderboardService svc) => _svc = svc;


        [HttpGet("students")]
        public async Task<ActionResult<StudentRankDto>> Students(
            [FromQuery] string metric = "quiz", [FromQuery] string period = "all", [FromQuery] int top = 50)
            => Ok(await _svc.GetGlobalStudentsAsync(metric, period, top));

        [HttpGet("teachers")]
        public async Task<ActionResult<GlobalLeaderboardResponse<TeacherRankDto>>> Teachers(
            [FromQuery] string metric = "quiz", [FromQuery] string period = "all", [FromQuery] int top = 50)
            => Ok(await _svc.GetGlobalTeachersAsync(metric, period, top));

        [HttpGet("schools")]
        public async Task<ActionResult<GlobalLeaderboardResponse<SchoolRankDto>>> Schools(
            [FromQuery] string metric = "quiz", [FromQuery] string period = "all", [FromQuery] int top = 50)
            => Ok(await _svc.GetGlobalSchoolsAsync(metric, period, top));
    }
}
