using System;
using System.Linq;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/parents")]
    [Authorize(Roles = "SchoolAdmin")]
    public class ParentsController : ControllerBase
    {
        private readonly IParentLinkRepository _repo;
        public ParentsController(IParentLinkRepository repo) => _repo = repo;

        [HttpPost("link")]
        public async Task<IActionResult> Add([FromBody] AddParentLinkDto dto)
        {
            await _repo.AddAsync(dto.StudentId, dto.ParentEmail, dto.IsPrimary);
            return NoContent();
        }

        [HttpDelete("link/{id:guid}")]
        public async Task<IActionResult> Remove(Guid id)
        {
            await _repo.RemoveAsync(id);
            return NoContent();
        }

        [HttpGet("links/{studentId:guid}")]
        public async Task<IActionResult> Get(Guid studentId)
        {
            var links = await _repo.GetByStudentAsync(studentId);
            return Ok(links);
        }
    }
}
