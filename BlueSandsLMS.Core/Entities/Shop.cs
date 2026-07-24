using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public enum OrderStatus { Pending = 0, Confirmed = 1, Shipped = 2, Delivered = 3, Cancelled = 4 }

    public class Product
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [MaxLength(200)] public string Name { get; set; } = "";
        [MaxLength(2000)] public string Description { get; set; } = "";
        public decimal Price { get; set; }
        [MaxLength(10)] public string Currency { get; set; } = "NGN";
        [MaxLength(100)] public string Category { get; set; } = "";
        [MaxLength(1000)] public string? ImageUrl { get; set; }
        public int StockCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Order
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        [MaxLength(10)] public string Currency { get; set; } = "NGN";
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Product? Product { get; set; }
        public User? User { get; set; }
    }
}
