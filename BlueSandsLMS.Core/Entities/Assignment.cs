using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class Assignment
    {
        [Key] public Guid Id { get; set; }
        public Guid ClassroomId { get; set; }
        [MaxLength(200)] public string Title { get; set; } = "";
        public AssignmentType Type { get; set; } = AssignmentType.Experiment;
        [MaxLength(100)] public string? ResourceCode { get; set; } // lab/quiz code
        public DateTime? DueAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Classroom? Classroom { get; set; }
    }

    public enum AssignmentType { Experiment = 0, Quiz = 1, Lesson = 2, Test = 3, Exam = 4 }
}
