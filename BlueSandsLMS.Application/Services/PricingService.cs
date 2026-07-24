using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services
{
    public interface IPricingService
    {

        Task<(decimal perStudent, PricingTier? tier)> ResolvePerStudentAsync(int studentCount, DateTime asOf, string? promoCode = null);
        (decimal subtotal, decimal vat, decimal total) ComputeTotals(int students, decimal perStudent, DateTime asOf);
        bool IsVatExempt(DateTime asOf);
    }

    public sealed class PricingService : IPricingService
    {
        private readonly BlueSandsLMSDbContext _db;
        public PricingService(BlueSandsLMSDbContext db) => _db = db;

        public async Task<(decimal perStudent, PricingTier? tier)> ResolvePerStudentAsync(int studentCount, DateTime asOf, string? promoCode = null)
        {

            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                var code = await _db.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code == promoCode.Trim());

                if (code == null)
                    throw new InvalidOperationException($"Promo code '{promoCode}' is not valid.");
                if (!code.IsActive)
                    throw new InvalidOperationException($"Promo code '{promoCode}' is no longer active.");
                if (code.ExpiresAt.HasValue && code.ExpiresAt.Value < asOf)
                    throw new InvalidOperationException($"Promo code '{promoCode}' has expired.");
                if (code.MaxRedemptions.HasValue && code.RedemptionCount >= code.MaxRedemptions.Value)
                    throw new InvalidOperationException($"Promo code '{promoCode}' has reached its redemption limit.");


                var pricingPromo = await _db.PricingPromos
                    .Where(pp => pp.UsePromoPricing &&
                                 (pp.StartsAt == null || pp.StartsAt <= asOf) &&
                                 (pp.EndsAt == null || pp.EndsAt >= asOf))
                    .OrderByDescending(pp => pp.DateCreated)
                    .FirstOrDefaultAsync();

                var promoPrice = pricingPromo?.PromoPricePerStudent ?? 2000m;
                return (promoPrice, null);
            }


            var tier = await _db.PricingTiers
                .Where(t => studentCount >= t.MinStudents && studentCount <= t.MaxStudents)
                .OrderBy(t => t.MinStudents)
                .FirstOrDefaultAsync();

            if (tier == null) throw new InvalidOperationException($"No pricing tier matches {studentCount} students.");
            return (tier.PricePerStudent, tier);
        }

        public bool IsVatExempt(DateTime asOf) => asOf >= new DateTime(2026, 01, 01);

        public (decimal subtotal, decimal vat, decimal total) ComputeTotals(int students, decimal perStudent, DateTime asOf)
        {
            var subtotal = perStudent * students;
            var vat = IsVatExempt(asOf) ? 0m : Math.Round(0.075m * subtotal, 2, MidpointRounding.AwayFromZero);
            var total = subtotal + vat;
            return (subtotal, vat, total);
        }
    }
}
