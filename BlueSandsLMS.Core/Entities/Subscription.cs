using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{
    public class Subscription : BaseEntity
    {
        public Guid SchoolId { get; set; }
        public Guid? UserId { get; set; }      
        public int StudentsCovered { get; set; }
        public decimal PricePerStudent { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public bool Active { get; set; }
        public string? LastPaymentReference { get; set; }
    }
}