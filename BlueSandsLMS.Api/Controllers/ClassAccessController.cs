using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/class-access")]
    [Authorize] // any authenticated user
    public class ClassAccessController : ControllerBase
    {
        private readonly IClassRepository _repo;
        public ClassAccessController(IClassRepository repo) => _repo = repo;

        private Guid UserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        /// <summary>List classes I belong to (as Teacher or Student)</summary>
        [HttpGet("mine")]
        public async Task<ActionResult<ClassSummaryDto[]>> Mine()
        {
            var list = await _repo.GetMyClassesAsync(UserId());
            return Ok(list.ToArray());
        }

        /// <summary>Rotate invite code for a class (teacher only)</summary>
        [HttpPost("{classId:guid}/rotate-invite")]
        [Authorize(Roles = "Teacher,SchoolAdmin")]
        public async Task<IActionResult> RotateInvite(Guid classId, [FromBody] RotateInviteCodeDto body)
        {
            // only teacher of this class (or school admin who is also teacher, per your policy)
            var me = UserId();
            var isTeacher = await _repo.UserIsTeacherAsync(classId, me);
            if (!isTeacher) return Forbid();

            var (code, expires) = await _repo.RotateInviteCodeAsync(classId, body.ExpireDays <= 0 ? 14 : body.ExpireDays);
            return Ok(new { code, expiresAt = expires });
        }

        /// <summary>Join a class by invite code (students)</summary>
        [HttpPost("join")]
        [Authorize(Roles = "Student,Teacher,SchoolAdmin")] // allow joining for any non-admin if you want; usually Students
        public async Task<IActionResult> Join([FromBody] JoinByCodeDto body)
        {
            if (string.IsNullOrWhiteSpace(body.Code)) return BadRequest("code required.");
            await _repo.JoinByCodeAsync(UserId(), body.Code);
            return NoContent();
        }
    }
}
