using System.Linq;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/admin/pricing-tiers")]
    [Authorize(Roles = "Admin")]
    public sealed class PricingTiersController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        public PricingTiersController(BlueSandsLMSDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTierDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TierName)) return BadRequest(new { message = "Tier Name is required." });
            if (dto.MinStudents < 1) return BadRequest(new { message = "Minimum Students must be ≥ 1." });
            if (dto.MaxStudents <= dto.MinStudents) return BadRequest(new { message = "Maximum Students must be > Minimum Students." });
            if (dto.PricePerStudent < 0) return BadRequest(new { message = "Price must be ≥ 0." });


            var overlap = await _db.PricingTiers
                .Where(t => !(dto.MaxStudents < t.MinStudents || dto.MinStudents > t.MaxStudents))
                .OrderBy(t => t.MinStudents)
                .FirstOrDefaultAsync();

            if (overlap != null)
                return BadRequest(new { message = $"Range {dto.MinStudents}-{dto.MaxStudents} overlaps with existing tier '{overlap.MinStudents}-{overlap.MaxStudents}'. Please adjust the range." });

            _db.PricingTiers.Add(new PricingTier
            {
                TierName = dto.TierName.Trim(),
                MinStudents = dto.MinStudents,
                MaxStudents = dto.MaxStudents,
                PricePerStudent = dto.PricePerStudent
            });
            await _db.SaveChangesAsync();
            return Ok(new { message = "Tier saved." });
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var items = await _db.PricingTiers
                .OrderBy(t => t.MinStudents)
                .Select(t => new TierListItemDto(t.Id, t.TierName, t.MinStudents, t.MaxStudents, t.PricePerStudent))
                .ToListAsync();
            return Ok(items);
        }
    }
}
