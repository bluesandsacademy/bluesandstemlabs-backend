using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.DTOs.Dashboard;
using BlueSandsLMS.Common.Interfaces.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/student/v1/leaderboard")]
    [Authorize(Roles = "Student,SchoolAdmin,Teacher,GlobalAdmin")]
    public class StudentV1LeaderboardController : ControllerBase
    {
        private readonly IStudentLeaderboardService _svc;
        
        public StudentV1LeaderboardController(IStudentLeaderboardService svc) => _svc = svc;
        
        private Guid GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                      ?? User.FindFirstValue("sub")
                      ?? throw new UnauthorizedAccessException("No user id claim.");
            return Guid.Parse(sub);
        }
        
        [HttpGet]
        public async Task<ActionResult<LeaderboardDto>> Get(
            [FromQuery] string scope = "national", 
            [FromQuery] int take = 10,
            CancellationToken ct = default)
        {

            return Ok(await _svc.GetLeaderboardAsync(GetUserId(), scope, ct));
        }
    }
}