using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class Enrollment
    {
        [Key] public Guid Id { get; set; }
        public Guid ClassroomId { get; set; }
        public Guid UserId { get; set; }
        public ClassRole RoleInClass { get; set; } = ClassRole.Student;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Classroom? Classroom { get; set; }
        public User? User { get; set; }
    }
}
