using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/schools/users")]
    [Authorize(Roles = "SchoolAdmin,Admin,GlobalAdmin")]
    public class SchoolUsersController : ControllerBase
    {
        private readonly ISchoolAdminService _svc;
        private readonly BlueSandsLMSDbContext _db;

        public SchoolUsersController(ISchoolAdminService svc, BlueSandsLMSDbContext db)
        {
            _svc = svc;
            _db = db;
        }

        private Guid RequireSchoolId()
        {
            var s = User.FindFirstValue("SchoolId");
            if (!Guid.TryParse(s, out var id)) throw new Exception("SchoolId missing in token.");
            return id;
        }

        private Guid AdminUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }


        [HttpGet("teachers")]
        public async Task<IActionResult> ListTeachers([FromQuery] Guid? schoolId, CancellationToken ct)
        {
            var sid = schoolId ?? RequireSchoolId();
            var teacherRoleId = await _db.Roles
                .Where(r => r.Name == "Teacher")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            var teachers = await _db.Users
                .AsNoTracking()
                .Where(u => u.SchoolId == sid && u.RoleId == teacherRoleId && u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.Email, u.Phone, u.Country, u.DateCreated, u.IsEmailVerified })
                .OrderBy(u => u.FullName)
                .ToListAsync(ct);

            return Ok(teachers);
        }


        [HttpGet("students")]
        public async Task<IActionResult> ListStudents([FromQuery] Guid? schoolId, CancellationToken ct)
        {
            var sid = schoolId ?? RequireSchoolId();
            var studentRoleId = await _db.Roles
                .Where(r => r.Name == "Student")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            var students = await _db.Users
                .AsNoTracking()
                .Where(u => u.SchoolId == sid && u.RoleId == studentRoleId && u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.Email, u.Phone, u.Country, u.DateCreated, u.IsEmailVerified })
                .OrderBy(u => u.FullName)
                .ToListAsync(ct);

            return Ok(students);
        }


        [HttpPost("teachers/upsert")]
        public async Task<IActionResult> UpsertTeacher([FromBody] UpsertTeacherDto dto, [FromQuery] Guid? schoolId = null)
        {
            var sid = schoolId ?? RequireSchoolId();
            var res = await _svc.UpsertTeacherAsync(AdminUserId(), sid, dto);
            return Ok(res);
        }

        [HttpPost("teachers/bulk-upsert")]
        public async Task<IActionResult> BulkUpsertTeachers([FromBody] BulkUpsertTeachersDto dto, [FromQuery] Guid? schoolId = null)
        {
            var sid = schoolId ?? RequireSchoolId();
            var res = await _svc.BulkUpsertTeachersAsync(AdminUserId(), sid, dto);
            return Ok(res);
        }


        [HttpPost("students/upsert")]
        public async Task<IActionResult> UpsertStudent([FromBody] UpsertStudentDto dto, [FromQuery] Guid? schoolId = null)
        {
            var sid = schoolId ?? RequireSchoolId();
            var res = await _svc.UpsertStudentAsync(AdminUserId(), sid, dto);
            return Ok(res);
        }

        [HttpPost("students/bulk-upsert")]
        public async Task<IActionResult> BulkUpsertStudents([FromBody] BulkUpsertStudentsDto dto, [FromQuery] Guid? schoolId = null)
        {
            var sid = schoolId ?? RequireSchoolId();
            var res = await _svc.BulkUpsertStudentsAsync(AdminUserId(), sid, dto);
            return Ok(res);
        }


        [HttpPut("{id:guid}/role")]
        public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleDto dto, CancellationToken ct)
        {
            await _svc.AssignRoleAsync(id, dto.Role, ct);
            return NoContent();
        }
    }
}
