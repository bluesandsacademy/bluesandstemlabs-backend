using BlueSandsLMS.Api.Services;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/class-access")]
    public class ClassAccessController : ControllerBase
    {
        private readonly IClassRepository _repo;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<ClassAccessController> _logger;

        public ClassAccessController(IClassRepository repo, ICurrentUser currentUser,
            ILogger<ClassAccessController> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        private Guid UserId()
        {
            // throws if no authenticated user id is available
            return _currentUser.GetUserId();
        }

        [HttpGet("mine")]
        [Authorize(Roles = "Teacher,SchoolAdmin")]
        public async Task<ActionResult<ClassSummaryDto[]>> Mine()
        {
            var list = await _repo.GetMyClassesAsync(UserId());
            return Ok(list.ToArray());
        }


        [HttpPost("{classId:guid}/rotate-invite")]
        [Authorize(Roles = "Teacher,SchoolAdmin")]
        public async Task<IActionResult> RotateInvite(Guid classId, [FromBody] RotateInviteCodeDto body)
        {
            var me = UserId();
            var isTeacher = await _repo.UserIsTeacherAsync(classId, me);
            if (!isTeacher) return Forbid();

            var (code, expires) = await _repo.RotateInviteCodeAsync(classId, body.ExpireDays <= 0 ? 14 : body.ExpireDays);
            return Ok(new { code, expiresAt = expires });
        }

        [HttpPost("join")]
        [Authorize(Roles = "Student,Teacher,SchoolAdmin")]
        public async Task<IActionResult> Join([FromBody] JoinByCodeDto body)
        {
            if (string.IsNullOrWhiteSpace(body.Code)) return BadRequest("code required.");
            await _repo.JoinByCodeAsync(UserId(), body.Code);
            return NoContent();
        }

        [HttpPost("{classId:guid}/attach-teacher")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> AttachTeacher(Guid classId, [FromBody] AttachTeacherDto dto)
        {
            if (dto == null || dto.TeacherId == Guid.Empty) return BadRequest("teacherId required.");

            var schoolId = _currentUser.SchoolId;
            if (schoolId == null) return Forbid();

            var classes = await _repo.GetClassesBySchoolIdAsync(schoolId.Value);
            if (!classes.Any(c => c.Id == classId)) return Forbid();

            await _repo.AttachTeacherAsync(classId, dto.TeacherId);
            return NoContent();
        }
    }
}