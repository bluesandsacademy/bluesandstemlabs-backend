using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class Announcement
    {
        [Key] public Guid Id { get; set; }
        public Guid? SchoolId { get; set; }     // null => global
        public Guid? ClassroomId { get; set; }  // null => school-wide/global
        [MaxLength(200)] public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Classroom? Classroom { get; set; }
        public School? School { get; set; }
    }
}
