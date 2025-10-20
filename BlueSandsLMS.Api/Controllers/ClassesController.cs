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
    [Route("api/classes")]
    [Authorize(Roles = "Teacher,SchoolAdmin")]
    public class ClassesController : ControllerBase
    {
        private readonly IClassRepository _repo;
        public ClassesController(IClassRepository repo) => _repo = repo;

        private Guid UserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }
        private Guid RequireSchoolId()
        {
            var s = User.FindFirstValue("SchoolId");
            if (!Guid.TryParse(s, out var id)) throw new Exception("SchoolId missing in token.");
            return id;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClassDto dto)
        {
            var schoolId = RequireSchoolId();
            var id = await _repo.CreateAsync(schoolId, UserId(), dto.Name, dto.Subject);
            return Ok(new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassDto dto)
        {
            if (!await _repo.UserIsTeacherAsync(id, UserId())) return Forbid();
            await _repo.UpdateAsync(id, dto.Name, dto.Subject);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await _repo.UserIsTeacherAsync(id, UserId())) return Forbid();
            await _repo.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id:guid}/enroll")]
        public async Task<IActionResult> Enroll(Guid id, [FromBody] EnrollByEmailDto dto)
        {
            if (!await _repo.UserIsTeacherAsync(id, UserId())) return Forbid();
            await _repo.EnrollByEmailAsync(id, dto.Email);
            return NoContent();
        }

        [HttpPost("{id:guid}/bulk-enroll")]
        public async Task<IActionResult> BulkEnroll(Guid id, [FromBody] BulkEnrollDto dto)
        {
            if (!await _repo.UserIsTeacherAsync(id, UserId())) return Forbid();
            await _repo.BulkEnrollAsync(id, dto.Emails ?? Enumerable.Empty<string>());
            return NoContent();
        }
    }
}
