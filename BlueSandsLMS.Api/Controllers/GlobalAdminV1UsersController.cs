using BlueSandsLMS.Application.Services;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/users")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1UsersController : ControllerBase
    {
        private readonly IGlobalAdminService _svc;
        private readonly IAuthService _auth;

        public GlobalAdminV1UsersController(IGlobalAdminService svc, IAuthService auth)
        {
            _svc = svc;
            _auth = auth;
        }

        [HttpGet]
        public async Task<ActionResult<BlueSandsLMS.Common.DTOs.PagedResult<GlobalAdminUserRowDto>>> Search([FromQuery] UserQuery query, CancellationToken ct)
            => Ok(await _svc.SearchUsersAsync(query, ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GlobalAdminUserRowDto>> Get(Guid id, CancellationToken ct)
        {
            var dto = await _svc.GetUserAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        // New: create a user (only GlobalAdmin)
        [HttpPost]
        public async Task<ActionResult<AuthResponseDto>> Create([FromBody] AdminCreateUserDto dto)
        {
            try
            {
                var res = await _auth.AdminCreateUserAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/activate")]
        public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        { await _svc.SetUserActiveAsync(id, true, ct); return NoContent(); }

        [HttpPost("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        { await _svc.SetUserActiveAsync(id, false, ct); return NoContent(); }

        [HttpPut("{id:guid}/role")]
        public async Task<IActionResult> SetRole(Guid id, [FromBody] SetUserRoleRequest req, CancellationToken ct)
        { await _svc.SetUserRoleAsync(id, req.RoleId, ct); return NoContent(); }

        [HttpPost("{id:guid}/reset-password")]
        public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(Guid id, CancellationToken ct)
            => Ok(await _svc.ResetPasswordAsync(id, ct));
    }
}       