using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class AuditEvent
    {
        [Key] public Guid Id { get; set; }
        public DateTime Utc { get; set; } = DateTime.UtcNow;
        public Guid? ActorUserId { get; set; }
        public Guid? SchoolId { get; set; }
        [MaxLength(50)] public string Category { get; set; } = ""; // auth|lti|email|report|usage
        [MaxLength(100)] public string Name { get; set; } = "";     // e.g., LtiLaunch.Ok
        public decimal? Value { get; set; }                         // optional numeric
        public string? ExtraJson { get; set; }                      // optional detail
    }
}
