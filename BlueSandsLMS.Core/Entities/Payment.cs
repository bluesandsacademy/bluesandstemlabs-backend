using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{
    public enum PaymentStatus { Pending = 0, Paid = 1, Failed = 2, Refunded = 3 }

    public class Payment : BaseEntity
    {
        public Guid SchoolId { get; set; }
        public Guid? UserId { get; set; }              // initiator (optional)
        public string Provider { get; set; } = "paystack";
        public string Reference { get; set; } = "";    // our ref: BS-...
        public string Currency { get; set; } = "NGN";
        public long AmountKobo { get; set; }           // Paystack expects kobo
        public decimal Subtotal { get; set; }          // NGN
        public decimal Vat { get; set; }               // NGN
        public decimal Total { get; set; }             // NGN
        public int StudentsBilled { get; set; }
        public decimal PricePerStudent { get; set; }
        public string? PromoCode { get; set; }         // e.g., "ABJ EDTECH 2025"
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string RawResponse { get; set; } = "";  // audit JSON
    }
}
