using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class QuizAttempt
    {
        [Key] public Guid Id { get; set; }
        
        public Guid UserId { get; set; }
        public Guid? ClassroomId { get; set; }
        public string Subject { get; set; } = "";
     public string QuizCode { get; set; } = default!;
      public bool Passed { get; set; }
        public decimal Score0to1 { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
         public string? Type { get; set; }              // pre|post
    public Guid? ExperimentLaunchId { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public Classroom? Classroom { get; set; }
        public User? User { get; set; }
    }
}
