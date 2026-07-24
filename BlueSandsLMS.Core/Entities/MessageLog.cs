
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSandsLMS.Core.Entities
{
    public class MessageLog
    {
        [Key] public Guid Id { get; set; }

        public Guid FromUserId { get; set; }
        public Guid? ToUserId { get; set; }
        public Guid? ClassroomId { get; set; }
        [MaxLength(200)] public string Channel { get; set; } = "inbox";

        [MaxLength(5000)] public string Body { get; set; } = "";

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }


        public Guid? ThreadId { get; set; }


        public Classroom? Classroom { get; set; }
        public User? FromUser { get; set; }
        public User? ToUser { get; set; }
    }
}
