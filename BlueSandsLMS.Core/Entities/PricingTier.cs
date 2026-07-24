using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{
    public class PricingTier : BaseEntity
    {
        public string TierName { get; set; } = "";
        public int MinStudents { get; set; }
        public int MaxStudents { get; set; }
        public decimal PricePerStudent { get; set; }
    }
}
