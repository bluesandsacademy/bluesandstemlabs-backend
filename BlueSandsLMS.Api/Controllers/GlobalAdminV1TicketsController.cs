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
    [Route("api/globaladmin/v1/tickets")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1TicketsController : ControllerBase
    {
        private readonly IGlobalTicketService _svc;
        public GlobalAdminV1TicketsController(IGlobalTicketService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] TicketQuery query, CancellationToken ct)
            => Ok(await _svc.SearchAsync(query, ct));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTicketRequest req, CancellationToken ct)
        {
            var id = await _svc.CreateAsync(req, ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTicketRequest req, CancellationToken ct)
        {
            await _svc.UpdateAsync(id, req, ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/comments")]
        public async Task<IActionResult> Comment(Guid id, [FromBody] AddTicketCommentRequest req, CancellationToken ct)
        {
            await _svc.AddCommentAsync(id, req, ct);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
