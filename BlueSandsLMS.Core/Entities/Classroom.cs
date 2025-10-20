using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class Classroom
    {
        [Key] public Guid Id { get; set; }
        public Guid SchoolId { get; set; }
        [MaxLength(200)] public string Name { get; set; } = "";
        [MaxLength(100)] public string Subject { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public School? School { get; set; }

        public string? InviteCode { get; set; }
    public DateTime? InviteCodeExpiresAt { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }

    public enum ClassRole { Student = 0, Teacher = 1 }
}
