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
    [Route("api/assignments")]
    [Authorize(Roles = "Teacher")]
    public class AssignmentsController : ControllerBase
    {
        private readonly IAssignmentRepository _repo;
        private readonly IClassRepository _classRepo;

        public AssignmentsController(IAssignmentRepository repo, IClassRepository classRepo)
        {
            _repo = repo;
            _classRepo = classRepo;
        }

        private Guid UserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
        {
            if (!await _classRepo.UserIsTeacherAsync(dto.ClassroomId, UserId())) return Forbid();
            var id = await _repo.CreateAsync(
                dto.ClassroomId, dto.Title, (int)dto.Type, dto.ResourceCode, dto.DueAt, UserId());
            return Ok(new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssignmentDto dto)
        {
            var classId = await _repo.GetClassroomIdAsync(id) ?? Guid.Empty;
            if (classId == Guid.Empty) return NotFound();
            if (!await _classRepo.UserIsTeacherAsync(classId, UserId())) return Forbid();

            await _repo.UpdateAsync(id, dto.Title, (int)dto.Type, dto.ResourceCode, dto.DueAt);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var classId = await _repo.GetClassroomIdAsync(id) ?? Guid.Empty;
            if (classId == Guid.Empty) return NotFound();
            if (!await _classRepo.UserIsTeacherAsync(classId, UserId())) return Forbid();

            await _repo.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("{classId:guid}/to-grade")]
        public async Task<IActionResult> ToGrade(Guid classId, int take = 20, int skip = 0)
        {
            if (!await _classRepo.UserIsTeacherAsync(classId, UserId())) return Forbid();
            take = Math.Clamp(take, 1, 100);
            skip = Math.Max(skip, 0);

            var items = await _repo.GetToGradeAsync(classId, take, skip);
            return Ok(items);
        }
    }
}
