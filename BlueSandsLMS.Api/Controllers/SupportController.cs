using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{

    public sealed record SubmitSupportTicketRequest(
        [Required, MaxLength(200)] string Subject,
        [Required, MaxLength(4000)] string Message,
        SupportCategory Category = SupportCategory.Other
    );

    [ApiController]
    [Route("api/support")]
    public sealed class SupportController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        public SupportController(BlueSandsLMSDbContext db) => _db = db;

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private string CurrentRole() =>
            User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "";

        private string ResolveCurrentUserType()
        {
            if (User.IsInRole("Teacher")) return "Teacher";
            if (User.IsInRole("Student")) return "Student";
            if (User.IsInRole("SchoolAdmin")) return "SchoolAdmin";
            if (User.IsInRole("GlobalAdmin")) return "GlobalAdmin";

            var role = CurrentRole().Trim();
            return string.IsNullOrWhiteSpace(role) ? "Unknown" : role;
        }

        [HttpPost("ticket")]
        [Authorize(Roles = "Teacher,Student,SchoolAdmin,GlobalAdmin")]
        public async Task<IActionResult> CreateTicket([FromBody] SubmitSupportTicketRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Message))
                return BadRequest(new { message = "Subject and Message are required." });

            var ticket = new SupportTicket
            {
                UserId   = CurrentUserId(),
                UserType = ResolveCurrentUserType(),
                Subject  = req.Subject.Trim(),
                Message  = req.Message.Trim(),
                Category = req.Category,
                Status   = SupportTicketStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportTickets.Add(ticket);
            await _db.SaveChangesAsync(ct);

            return Ok(new { id = ticket.Id, status = ticket.Status.ToString(), message = "Support ticket created." });
        }

        [HttpGet("resources")]
        [Authorize]
        public async Task<IActionResult> Resources([FromQuery] string? category, CancellationToken ct)
        {
            var query = _db.SupportResources.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(r => r.Category == category);

            var items = await query
                .OrderBy(r => r.Category).ThenBy(r => r.Title)
                .Select(r => new { r.Id, r.Title, r.Description, r.Url, r.Category, r.CreatedAt })
                .ToListAsync(ct);

            return Ok(items);
        }
    }
}
