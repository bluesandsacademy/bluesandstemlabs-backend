using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BlueSandsLMS.Core.Common;

namespace BlueSandsLMS.Core.Entities
{
    public enum TicketStatus { Open = 0, Pending = 1, Resolved = 2, Closed = 3 }
    public enum TicketPriority { Low = 0, Medium = 1, High = 2, Urgent = 3 }
    public enum TicketSource { Student = 0, Teacher = 1, SchoolAdmin = 2, GlobalAdmin = 3, System = 4 }

    public class Ticket : BaseEntity
    {
        public Guid? SchoolId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? AssignedToUserId { get; set; }

        [MaxLength(200)] public string Subject { get; set; } = "";
        [MaxLength(8000)] public string Body { get; set; } = "";

        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public TicketSource Source { get; set; } = TicketSource.System;

        [MaxLength(200)] public string? TagsCsv { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }


        public School? School { get; set; }
        public User? CreatedByUser { get; set; }
        public User? AssignedToUser { get; set; }
        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    }

    public class TicketComment : BaseEntity
    {
        public Guid TicketId { get; set; }
        public Guid UserId { get; set; }

        [MaxLength(8000)] public string Body { get; set; } = "";
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Ticket? Ticket { get; set; }
        public User? User { get; set; }
    }
}
