using System.Security.Claims;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/teacher/ils")]
    [Authorize(Roles = "Teacher")]
    public sealed class TeacherIlsProgressController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;

        public TeacherIlsProgressController(BlueSandsLMSDbContext db) => _db = db;

        [HttpGet("{id:guid}/progress")]
        public async Task<IActionResult> GetProgress(Guid id, CancellationToken ct)
        {
            var teacherId = CurrentUserId();
            if (teacherId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var ils = await _db.InteractiveLearningSpaces
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (ils == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ILS not found.");
            if (ils.CreatedBy != teacherId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only access progress for ILS resources you own.");

            var rows = await _db.StudentIlsSessions
                .AsNoTracking()
                .Where(x => x.IlsId == id)
                .Select(x => new
                {
                    studentId = x.StudentId,
                    currentStep = x.CurrentStep,
                    score = _db.SessionAssessments
                        .Where(a => a.SessionId == x.Id)
                        .Select(a => (decimal?)a.Score)
                        .FirstOrDefault(),
                    flaggedMessageCount = _db.IlsDiscussionMessages
                        .Count(m => m.SessionId == x.Id && m.Flagged)
                })
                .OrderBy(x => x.studentId)
                .ToListAsync(ct);

            return Ok(rows);
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
