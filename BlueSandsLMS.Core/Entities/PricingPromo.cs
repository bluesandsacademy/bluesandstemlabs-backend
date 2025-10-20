using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{
    public class PricingPromo : BaseEntity
    {
        public bool UsePromoPricing { get; set; } = true;
        public decimal PromoPricePerStudent { get; set; } = 2000m; // current promo
        public DateTime? StartsAt { get; set; } = null;            // null = always
        public DateTime? EndsAt { get; set; } = null;
    }
}
