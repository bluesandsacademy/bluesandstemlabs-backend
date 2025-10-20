// BlueSandsLMS.Core/Entities/MessageLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSandsLMS.Core.Entities
{
    public class MessageLog
    {
        [Key] public Guid Id { get; set; }

        public Guid FromUserId { get; set; }
        public Guid? ToUserId { get; set; }                 // DM or null for broadcast
        public Guid? ClassroomId { get; set; }              // scope to a class if applicable
        [MaxLength(200)] public string Channel { get; set; } = "inbox"; // inbox|announcement|dm|forum-msg (optional)

        [MaxLength(5000)] public string Body { get; set; } = "";

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }               // when receiver reads (DM); null for unread

        // reply/threads (optional)
        public Guid? ThreadId { get; set; }

        // navs
        public Classroom? Classroom { get; set; }
        public User? FromUser { get; set; }
        public User? ToUser { get; set; }
    }
}
