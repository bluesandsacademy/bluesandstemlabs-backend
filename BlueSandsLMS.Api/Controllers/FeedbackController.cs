using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{

    public sealed record SubmitFeedbackRequest(
        [Required, MaxLength(1000)] string Message,
        FeedbackCategory Category = FeedbackCategory.General
    );

    public sealed record UpdateFeedbackStatusRequest(FeedbackStatus Status);

    [ApiController]
    public sealed class FeedbackController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        public FeedbackController(BlueSandsLMSDbContext db) => _db = db;

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private string CurrentRole() =>
            User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "";

        [HttpPost("api/feedback")]
        [Authorize]
        public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Message))
                return BadRequest(new { message = "Message is required." });

            var feedback = new Feedback
            {
                UserId   = CurrentUserId(),
                UserType = CurrentRole(),
                Message  = req.Message.Trim(),
                Category = req.Category,
                Status   = FeedbackStatus.Pending
            };

            _db.Feedbacks.Add(feedback);
            await _db.SaveChangesAsync(ct);

            return Ok(new { id = feedback.Id, message = "Feedback submitted. Thank you!" });
        }

        [HttpGet("api/admin/feedback")]
        [Authorize(Roles = "GlobalAdmin")]
        public async Task<IActionResult> List(
            [FromQuery] FeedbackCategory? category,
            [FromQuery] string?           userType,
            [FromQuery] FeedbackStatus?   status,
            [FromQuery] int               page     = 1,
            [FromQuery] int               pageSize = 20,
            CancellationToken ct = default)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.Feedbacks.AsNoTracking()
                .Where(f => !f.IsDeleted);

            if (category.HasValue)
                query = query.Where(f => f.Category == category.Value);
            if (!string.IsNullOrWhiteSpace(userType))
                query = query.Where(f => f.UserType == userType);
            if (status.HasValue)
                query = query.Where(f => f.Status == status.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(f => f.DateCreated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new
                {
                    f.Id,
                    f.UserId,
                    f.UserType,
                    f.Message,
                    f.Category,
                    f.Status,
                    f.DateCreated
                })
                .ToListAsync(ct);

            return Ok(new
            {
                total,
                page,
                pageSize,
                items
            });
        }

        [HttpPatch("api/admin/feedback/{id:guid}/status")]
        [Authorize(Roles = "GlobalAdmin")]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateFeedbackStatusRequest req,
            CancellationToken ct)
        {
            var feedback = await _db.Feedbacks.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct);
            if (feedback == null) return NotFound();

            feedback.Status = req.Status;
            await _db.SaveChangesAsync(ct);

            return Ok(new { id = feedback.Id, status = feedback.Status.ToString() });
        }
    }
}
