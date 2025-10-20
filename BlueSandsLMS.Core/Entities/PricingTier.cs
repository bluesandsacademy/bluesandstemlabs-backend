using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{
    public class PricingTier : BaseEntity
    {
        public string TierName { get; set; } = "";
        public int MinStudents { get; set; }      // inclusive
        public int MaxStudents { get; set; }      // inclusive
        public decimal PricePerStudent { get; set; } // NGN
    }
}
