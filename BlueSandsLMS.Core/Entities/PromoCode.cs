namespace BlueSandsLMS.Core.Entities
{
    public class PromoCode
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxRedemptions { get; set; }
        public int RedemptionCount { get; set; }
        // future: percentage or fixed amount discount fields
    }
}
