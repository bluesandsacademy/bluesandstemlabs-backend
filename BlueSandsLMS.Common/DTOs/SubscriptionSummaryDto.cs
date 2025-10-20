namespace BlueSandsLMS.Common.DTOs
{
    public sealed class SubscriptionSummaryDto
    {
        public bool Active { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int StudentsCovered { get; set; }
        public decimal PricePerStudent { get; set; }
        public string? LastPaymentReference { get; set; }
        public int DaysRemaining { get; set; }       // 0 if expired/null
        public bool IsExpired => EndsAt.HasValue && EndsAt.Value < DateTime.UtcNow;
    }

    public sealed class TierSummaryDto
    {
       public long Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        public int MinStudents { get; set; }
        public int MaxStudents { get; set; }
        public decimal PricePerStudent { get; set; }
        public bool IsMatch { get; set; }            // true if this tier matches current covered students
    }

}