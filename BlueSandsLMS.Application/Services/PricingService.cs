using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services
{
    public interface IPricingService
    {
        // ✅ accept promoCode
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
            // ✅ Only those who enter the code get ₦2,000
            if (!string.IsNullOrWhiteSpace(promoCode) &&
                string.Equals(promoCode.Trim(), "ABJEDTECH2025", StringComparison.OrdinalIgnoreCase))
            {
                return (2000m, null);
            }

            // 🚫 Ignore global promo so only code unlocks 2000 (as requested).
            // If you ever need global promo again, re-enable it here.

            // Fallback: pricing tiers (must be non-overlapping)
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
