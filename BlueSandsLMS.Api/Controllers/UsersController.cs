using BlueSandsLMS.Application.Services;
using BlueSandsLMS.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "GlobalAdmin,SchoolAdmin")]  // <-- Protected
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _auth;
        public UsersController(IAuthService auth) => _auth = auth;

        [HttpPost("create")]
        public async Task<IActionResult> Create(AdminCreateUserDto dto)
        {
            try { return Ok(await _auth.AdminCreateUserAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
