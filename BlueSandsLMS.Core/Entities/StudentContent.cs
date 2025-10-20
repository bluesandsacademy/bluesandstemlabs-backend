using System;
using System.Collections.Generic;

namespace BlueSandsLMS.Core.Entities
{
    public class Subject
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }

    public class Lesson
    {
        public Guid Id { get; set; }
        public string SubjectCode { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? Summary { get; set; }
        public int DurationMin { get; set; } = 10;
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public Subject? Subject { get; set; }
        public ICollection<LessonProgress> Progresses { get; set; } = new List<LessonProgress>();
    }

    public class LessonProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }

    // NEW
    public Guid? ClassroomId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public User? User { get; set; }
    public Lesson? Lesson { get; set; }

    // NEW
    public Classroom? Classroom { get; set; }
}

    public class Certificate
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string SubjectCode { get; set; } = default!;
        public string Title { get; set; } = default!;
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Subject? Subject { get; set; }
    }
}
