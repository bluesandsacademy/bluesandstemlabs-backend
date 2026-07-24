using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/tags")]
    [Authorize(Roles = "Teacher")]
    public class TagsController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;

        public TagsController(BlueSandsLMSDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var items = await _db.CurriculumTags
                .AsNoTracking()
                .OrderBy(x => x.Subject)
                .ThenBy(x => x.Label)
                .Select(x => new TagDto
                {
                    Id = x.Id,
                    Label = x.Label,
                    Subject = x.Subject
                })
                .ToListAsync(ct);

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTagRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Label) || string.IsNullOrWhiteSpace(request.Subject))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Label and subject are required.",
                    ("label", "Label is required"),
                    ("subject", "Subject is required"));
            }

            var label = request.Label.Trim();
            var subject = request.Subject.Trim();

            var duplicate = await _db.CurriculumTags.AnyAsync(
                x => x.Label.ToLower() == label.ToLower() && x.Subject.ToLower() == subject.ToLower(),
                ct);
            if (duplicate)
            {
                return Error(
                    StatusCodes.Status409Conflict,
                    "CONFLICT",
                    "A tag with the same label and subject already exists.");
            }

            var entity = new CurriculumTag
            {
                Id = Guid.NewGuid(),
                Label = label,
                Subject = subject,
                CreatedAt = DateTime.UtcNow
            };

            _db.CurriculumTags.Add(entity);
            await _db.SaveChangesAsync(ct);

            return StatusCode(StatusCodes.Status201Created, new TagDto
            {
                Id = entity.Id,
                Label = entity.Label,
                Subject = entity.Subject
            });
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
