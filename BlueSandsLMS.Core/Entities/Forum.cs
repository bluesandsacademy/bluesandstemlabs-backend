// BlueSandsLMS.Core/Entities/Forum.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class ForumTopic
    {
        [Key] public Guid Id { get; set; }
        public Guid ClassroomId { get; set; }               // forum per class
        [MaxLength(200)] public string Title { get; set; } = "";
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsLocked { get; set; }

        public Classroom? Classroom { get; set; }
        public User? CreatedBy { get; set; }
        public ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();
    }

    public class ForumPost
    {
        [Key] public Guid Id { get; set; }
        public Guid TopicId { get; set; }
        public Guid UserId { get; set; }
        [MaxLength(8000)] public string Body { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }

        public ForumTopic? Topic { get; set; }
        public User? User { get; set; }
    }
}
