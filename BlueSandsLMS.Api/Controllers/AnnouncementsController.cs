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
    [Route("api/announcements")]
    [Authorize(Roles = "Teacher,SchoolAdmin")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementRepository _repo;
        private readonly IClassRepository _classRepo;

        public AnnouncementsController(IAnnouncementRepository repo, IClassRepository classRepo)
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
        public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
        {

            if (User.IsInRole("Teacher"))
            {
                if (!await _classRepo.UserIsTeacherAsync(dto.ClassroomId, UserId())) return Forbid();
            }

            var id = await _repo.CreateAsync(dto.ClassroomId, UserId(), dto.Title, dto.Body);
            return Ok(new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnnouncementDto dto)
        {
            if (User.IsInRole("Teacher"))
            {
                var classId = await _repo.GetClassroomIdAsync(id) ?? Guid.Empty;
                if (classId == Guid.Empty) return NotFound();
                if (!await _classRepo.UserIsTeacherAsync(classId, UserId())) return Forbid();
            }

            await _repo.UpdateAsync(id, dto.Title, dto.Body);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (User.IsInRole("Teacher"))
            {
                var classId = await _repo.GetClassroomIdAsync(id) ?? Guid.Empty;
                if (classId == Guid.Empty) return NotFound();
                if (!await _classRepo.UserIsTeacherAsync(classId, UserId())) return Forbid();
            }

            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
