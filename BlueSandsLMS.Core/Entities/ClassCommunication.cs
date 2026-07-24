using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class ClassroomTeacher
    {
        public Guid ClassroomId { get; set; }
        public Guid TeacherUserId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public Classroom? Classroom { get; set; }
        public User? Teacher { get; set; }
    }

    public class ClassMessage
    {
        [Key] public Guid Id { get; set; }
        public Guid ClassroomId { get; set; }
        public Guid FromUserId { get; set; }
        [MaxLength(4000)] public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public Classroom? Classroom { get; set; }
        public User? FromUser { get; set; }
        public ICollection<ClassMessageRead> Reads { get; set; } = new List<ClassMessageRead>();
    }

    public class ClassMessageRead
    {
        public Guid MessageId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        public ClassMessage? Message { get; set; }
        public User? User { get; set; }
    }
}
