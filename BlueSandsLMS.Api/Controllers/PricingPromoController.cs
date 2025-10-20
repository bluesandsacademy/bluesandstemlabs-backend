using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/admin/pricing-promo")]
    [Authorize(Roles = "Admin")]
    public sealed class PricingPromoController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        public PricingPromoController(BlueSandsLMSDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] PromoUpsertDto dto)
        {
            if (dto.PromoPricePerStudent <= 0) return BadRequest(new { message = "Promo price must be > 0." });

            var current = await _db.PricingPromos.OrderByDescending(x => x.DateCreated).FirstOrDefaultAsync();
            if (current == null)
                _db.PricingPromos.Add(new PricingPromo {
                    UsePromoPricing = dto.UsePromoPricing,
                    PromoPricePerStudent = dto.PromoPricePerStudent,
                    StartsAt = dto.StartsAt, EndsAt = dto.EndsAt
                });
            else {
                current.UsePromoPricing = dto.UsePromoPricing;
                current.PromoPricePerStudent = dto.PromoPricePerStudent;
                current.StartsAt = dto.StartsAt; current.EndsAt = dto.EndsAt;
            }
            await _db.SaveChangesAsync();
            return Ok(new { message = "Promo updated." });
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var p = await _db.PricingPromos.OrderByDescending(x => x.DateCreated).FirstOrDefaultAsync();
            return Ok(p);
        }
    }
}
