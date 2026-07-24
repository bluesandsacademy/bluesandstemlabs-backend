using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/simulations")]
    [Authorize(Roles = "Teacher")]
    public class SimulationsController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IMemoryCache _cache;

        public SimulationsController(BlueSandsLMSDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? grade, [FromQuery] string? subject, CancellationToken ct)
        {
            var normalizedGrade = grade?.Trim().ToLowerInvariant() ?? string.Empty;
            var normalizedSubject = subject?.Trim().ToLowerInvariant() ?? string.Empty;
            var cacheKey = $"simulations:list:{normalizedGrade}:{normalizedSubject}";

            if (_cache.TryGetValue(cacheKey, out List<SimulationListItemDto>? cachedItems) && cachedItems != null)
                return Ok(cachedItems);

            var query = _db.PhETSimulations
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(subject))
            {
                var normalized = subject.Trim().ToLowerInvariant();
                if (normalized is "physics")
                    query = query.Where(x => x.Physics);
                else if (normalized is "chemistry")
                    query = query.Where(x => x.Chemistry);
                else if (normalized is "biology")
                    query = query.Where(x => x.Biology);
                else if (normalized is "earthspace" or "earth-space" or "earth_space" or "earth & space")
                    query = query.Where(x => x.EarthSpace);
                else if (normalized is "math" or "statistics" or "mathstatistics")
                    query = query.Where(x => x.MathStatistics);
                else
                    return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Invalid subject filter.");
            }

            if (!string.IsNullOrWhiteSpace(grade))
            {
                var g = grade.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    (x.GradeLevel != null && x.GradeLevel.ToLower().Contains(g)) ||
                    (x.LowGradeLevel != null && x.LowGradeLevel.ToLower() == g) ||
                    (x.HighGradeLevel != null && x.HighGradeLevel.ToLower() == g));
            }

            var items = await query
                .OrderBy(x => x.Title)
                .Select(x => new SimulationListItemDto
                {
                    Id = x.Id,
                    Name = x.Title,
                    PreviewUrl = x.ThumbnailUrl ?? x.SimPage ?? x.RunnableResource
                })
                .ToListAsync(ct);

            _cache.Set(
                cacheKey,
                items,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

            return Ok(items);
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
