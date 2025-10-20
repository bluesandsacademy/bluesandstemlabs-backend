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
    [Route("api/schools/users")]
    [Authorize(Roles = "SchoolAdmin,Admin,GlobalAdmin")]
    public class SchoolUsersController : ControllerBase
    {
        private readonly ISchoolAdminService _svc;
        public SchoolUsersController(ISchoolAdminService svc) => _svc = svc;

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

        // ---- Teachers ----
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

        // ---- Students ----
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
    }
}
