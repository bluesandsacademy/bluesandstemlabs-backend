using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public enum SupportCategory { Technical = 0, Billing = 1, Content = 2, Other = 3 }
    public enum SupportTicketStatus { Open = 0, InProgress = 1, Resolved = 2, Closed = 3 }

    public class SupportTicket
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }

        [MaxLength(200)] public string Subject { get; set; } = "";
        [MaxLength(4000)] public string Message { get; set; } = "";

        public SupportCategory Category { get; set; } = SupportCategory.Other;

        [MaxLength(50)] public string UserType { get; set; } = "";

        public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }

    public class SupportResource
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [MaxLength(200)] public string Title { get; set; } = "";
        [MaxLength(2000)] public string Description { get; set; } = "";
        [MaxLength(1000)] public string Url { get; set; } = "";
        [MaxLength(100)] public string Category { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
