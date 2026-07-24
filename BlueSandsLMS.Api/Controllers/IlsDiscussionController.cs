using System.Security.Claims;
using BlueSandsLMS.Api.Infrastructure;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/ils/{id:guid}/discussion")]
    [Authorize(Roles = "Student,Teacher")]
    public sealed class IlsDiscussionController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IPlainTextInputGuard _inputGuard;

        public IlsDiscussionController(BlueSandsLMSDbContext db, IPlainTextInputGuard inputGuard)
        {
            _db = db;
            _inputGuard = inputGuard;
        }

        [HttpGet]
        public async Task<IActionResult> GetDiscussion(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var role = CurrentRole();
            if (string.IsNullOrWhiteSpace(role))
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            if (page <= 0 || pageSize <= 0 || pageSize > 200)
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("page", "page must be greater than 0."),
                    ("pageSize", "pageSize must be between 1 and 200."));
            }

            var ils = await _db.InteractiveLearningSpaces
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (ils == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ILS not found.");

            if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase) && ils.CreatedBy != userId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only access discussion for ILS resources you own.");

            if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                var session = await _db.StudentIlsSessions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IlsId == id && x.StudentId == userId, ct);
                if (session == null)
                    return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

                var reflectionDone = session.ReflectionSubmitted ||
                                     await _db.SessionReflections.AsNoTracking().AnyAsync(x => x.SessionId == session.Id, ct);
                if (!reflectionDone)
                {
                    return Error(
                        StatusCodes.Status422UnprocessableEntity,
                        "BUSINESS_RULE_ERROR",
                        "Reflection must be submitted before discussion is available.");
                }
            }

            var query = _db.IlsDiscussionMessages
                .AsNoTracking()
                .Where(x => x.IlsId == id);

            if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => !x.Flagged);

            var totalCount = await query.CountAsync(ct);

            var messages = await query
                .OrderBy(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    id = x.Id,
                    author = x.Author != null ? x.Author.FullName : string.Empty,
                    text = x.Text,
                    flagged = x.Flagged,
                    createdAt = x.CreatedAt
                })
                .ToListAsync(ct);

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
            return Ok(messages);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> PostDiscussion(Guid id, [FromBody] DiscussionPostRequest request, CancellationToken ct)
        {
            if (request.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(request.Text))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("sessionId", "sessionId is required."),
                    ("text", "text is required."));
            }

            if (!_inputGuard.IsSafe(request.Text))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("text", "Discussion text contains unsupported characters or markup."));
            }

            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var session = await _db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");
            if (session.IlsId != id)
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Session does not belong to this ILS.");
            if (session.StudentId != userId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only post with your own session.");

            var reflectionDone = session.ReflectionSubmitted ||
                                 await _db.SessionReflections.AsNoTracking().AnyAsync(x => x.SessionId == session.Id, ct);
            if (!reflectionDone)
            {
                return Error(
                    StatusCodes.Status422UnprocessableEntity,
                    "BUSINESS_RULE_ERROR",
                    "Reflection must be submitted before posting discussion messages.");
            }

            var message = new BlueSandsLMS.Core.Entities.IlsDiscussionMessage
            {
                Id = Guid.NewGuid(),
                IlsId = id,
                SessionId = request.SessionId,
                AuthorId = userId,
                Text = request.Text.Trim(),
                Flagged = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.IlsDiscussionMessages.Add(message);
            await _db.SaveChangesAsync(ct);

            var author = await _db.Users.AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            return Ok(new
            {
                id = message.Id,
                author,
                text = message.Text,
                flagged = message.Flagged,
                createdAt = message.CreatedAt
            });
        }

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
        }

        private string CurrentRole() =>
            User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;

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
