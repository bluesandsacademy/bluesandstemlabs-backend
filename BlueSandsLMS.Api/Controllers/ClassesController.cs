using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/classes")]
  //  [Authorize(Roles = "Teacher,SchoolAdmin")]
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

        
        [HttpGet("school")]
        public async Task<IActionResult> BySchool([FromQuery] Guid? schoolId)
        {
            var claimSchool = RequireSchoolId();
            Guid actualSchool;

            if (schoolId.HasValue && schoolId.Value != claimSchool)
            {

                if (!User.IsInRole("SchoolAdmin"))
                    return Forbid();
                actualSchool = schoolId.Value;
            }
            else
            {
                actualSchool = claimSchool;
            }

            var list = await _repo.GetClassesBySchoolIdAsync(actualSchool);
            return Ok(list.ToArray());
        }

        [HttpPost("enroll")]
        public async Task<IActionResult> Enroll(Guid classId, [FromBody] EnrollByEmailDto dto)
        {
            if (!await _repo.UserIsTeacherAsync(classId, UserId())) return Forbid();
            var role = dto.Role == ClassRoleDto.Teacher ? ClassRole.Teacher : ClassRole.Student;
            await _repo.EnrollByEmailAsync(classId, dto.Email, role);
            return NoContent();
        }

        [HttpPost("bulk-enroll")]
        public async Task<IActionResult> BulkEnroll(Guid classId, [FromBody] BulkEnrollDto dto)
        {
            if (!await _repo.UserIsTeacherAsync(classId, UserId())) return Forbid();
            var role = dto.Role == ClassRoleDto.Teacher ? ClassRole.Teacher : ClassRole.Student;
            await _repo.BulkEnrollAsync(classId, dto.Emails ?? Enumerable.Empty<string>(), role);
            return NoContent();
        }

        [HttpPut("enroll")]
        public async Task<IActionResult> UpdateEnrollment(Guid oldClassId, [FromBody] TransferEnrollmentDto dto)
        {
            if (!await _repo.UserIsTeacherAsync(oldClassId, UserId())) return Forbid();

            ClassRole? role = dto.Role switch
            {
                ClassRoleDto.Teacher => ClassRole.Teacher,
                ClassRoleDto.Student => ClassRole.Student,
                _ => null
            };

            await _repo.TransferEnrollmentAsync(oldClassId, dto.Email, dto.NewClassId, role);
            return NoContent();
        }
    }
}
