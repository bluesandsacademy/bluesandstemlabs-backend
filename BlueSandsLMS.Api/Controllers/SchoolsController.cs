using BlueSandsLMS.Application.Services;
using BlueSandsLMS.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "GlobalAdmin")]
    public class SchoolsController : ControllerBase
    {
        private readonly ISchoolService _schoolService;

        public SchoolsController(ISchoolService schoolService)
        {
            _schoolService = schoolService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSchoolDto dto)
        {
            var result = await _schoolService.CreateAsync(dto);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _schoolService.GetAllAsync();
            return Ok(result);
        }
        [HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSchoolDto dto)
{
    var result = await _schoolService.UpdateAsync(id, dto);
    return Ok(result);
}

[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
{
    await _schoolService.DeleteAsync(id);
    return NoContent();
}

    }
}
