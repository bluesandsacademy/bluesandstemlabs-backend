using System.Security.Claims;
using System.Text.Json;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/discussion")]
    [Authorize(Roles = "Teacher")]
    public sealed class DiscussionModerationController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;

        public DiscussionModerationController(BlueSandsLMSDbContext db) => _db = db;

        [HttpPost("{msgId:guid}/flag")]
        public async Task<IActionResult> FlagMessage(Guid msgId, [FromBody] DiscussionFlagRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("reason", "reason is required."));
            }

            var teacherId = CurrentUserId();
            if (teacherId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var message = await _db.IlsDiscussionMessages
                .Include(x => x.Ils)
                .FirstOrDefaultAsync(x => x.Id == msgId, ct);
            if (message == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Discussion message not found.");
            if (message.Ils == null || message.Ils.CreatedBy != teacherId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only moderate discussion on ILS resources you own.");

            var now = DateTime.UtcNow;
            message.Flagged = true;
            message.FlagReason = request.Reason.Trim();
            message.FlaggedAt = now;
            message.FlaggedBy = teacherId;

            _db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                Utc = now,
                ActorUserId = teacherId,
                Category = "moderation",
                Name = "Discussion.Flagged",
                ExtraJson = JsonSerializer.Serialize(new
                {
                    messageId = message.Id,
                    message.IlsId,
                    message.SessionId,
                    reason = message.FlagReason
                })
            });

            await _db.SaveChangesAsync(ct);
            return Ok(new { flagged = true });
        }

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
        }

        private IActionResult Error(int statusCode, string code, string message, params (string field, string issue)[] details)
        {
            var payload = new
            {
                error = true,
                code,
                message,
                details = details.Select(d => new { field = d.field, issue = d.issue })
            };
            return StatusCode(statusCode, payload);
        }
    }
}
