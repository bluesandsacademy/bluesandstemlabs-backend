using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/users")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1UsersController : ControllerBase
    {
        private readonly IGlobalAdminService _svc;
        public GlobalAdminV1UsersController(IGlobalAdminService svc) => _svc = svc;

        [HttpGet]
        public async Task<ActionResult<PagedResult<GlobalAdminUserRowDto>>> Search([FromQuery] UserQuery query, CancellationToken ct)
            => Ok(await _svc.SearchUsersAsync(query, ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GlobalAdminUserRowDto>> Get(Guid id, CancellationToken ct)
        {
            var dto = await _svc.GetUserAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
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
