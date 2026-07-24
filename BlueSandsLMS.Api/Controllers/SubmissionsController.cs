using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/submissions")]
    [Authorize]
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionRepository _repo;
        public SubmissionsController(ISubmissionRepository repo) => _repo = repo;

        private Guid UserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }
        private string Role() => User.FindFirstValue(ClaimTypes.Role) ?? "";


        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit([FromBody] SubmitWorkDto dto)
        {
            try
            {
                var id = await _repo.SubmitAsync(dto.AssignmentId, UserId(), dto);
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        
        [HttpPut("{submissionId:guid}/resubmit")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Resubmit(Guid submissionId, [FromBody] ResubmitWorkDto dto)
        {
            try
            {
                await _repo.ResubmitAsync(submissionId, UserId(), dto);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("mine")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Mine([FromQuery] Guid assignmentId)
        {
            var row = await _repo.GetMineAsync(assignmentId, UserId());
            return Ok(row);
        }


        [HttpPut("{submissionId:guid}/grade")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Grade(Guid submissionId, [FromBody] GradeSubmissionDto dto)
        {
            try
            {
                await _repo.GradeAsync(submissionId, UserId(), dto.Score0to1, dto.Feedback);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("by-assignment/{assignmentId:guid}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ByAssignment(Guid assignmentId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {

            var ok = await _repo.IsTeacherOfAssignmentAsync(assignmentId, UserId());
            if (!ok) return Forbid();

            take = Math.Clamp(take, 1, 200);
            skip = Math.Max(0, skip);

            var rows = await _repo.ListByAssignmentAsync(assignmentId, skip, take);
            return Ok(rows);
        }
    }
}
