using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{
    public enum PaymentStatus { Pending = 0, Paid = 1, Failed = 2, Refunded = 3 }

    public class Payment : BaseEntity
    {
        public Guid SchoolId { get; set; }
        public Guid? UserId { get; set; }
        public string Provider { get; set; } = "paystack";
        public string Reference { get; set; } = "";
        public string Currency { get; set; } = "NGN";
        public long AmountKobo { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public int StudentsBilled { get; set; }
        public decimal PricePerStudent { get; set; }
        public string? PromoCode { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string RawResponse { get; set; } = "";
    }
}
