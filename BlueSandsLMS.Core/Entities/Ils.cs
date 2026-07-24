using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public enum IlsStatus
    {
        Draft = 0,
        Published = 1
    }

    public class InteractiveLearningSpace
    {
        [Key] public Guid Id { get; set; }
        [MaxLength(200)] public string Title { get; set; } = string.Empty;
        [MaxLength(2000)] public string Objective { get; set; } = string.Empty;
        [MaxLength(50)] public string Grade { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public Guid? SimulationId { get; set; }
        public string SimulationSource { get; set; } = "phet";
        public string? PraxiLabsExperimentId { get; set; }
        public string? PollOptionsJson { get; set; }
        public string? AssessmentConfigJson { get; set; }
        public IlsStatus Status { get; set; } = IlsStatus.Draft;
        public Guid CreatedBy { get; set; }
        public Guid? SchoolId { get; set; }
        public bool IsSharedWithSchool { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public PhETSimulation? Simulation { get; set; }
        public User? CreatedByUser { get; set; }
        public School? School { get; set; }
        public ICollection<IlsTag> Tags { get; set; } = new List<IlsTag>();
        public ICollection<StudentIlsSession> Sessions { get; set; } = new List<StudentIlsSession>();
        public ICollection<IlsDiscussionMessage> DiscussionMessages { get; set; } = new List<IlsDiscussionMessage>();
    }

    public class CurriculumTag
    {
        [Key] public Guid Id { get; set; }
        [MaxLength(120)] public string Label { get; set; } = string.Empty;
        [MaxLength(80)] public string Subject { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<IlsTag> Ils { get; set; } = new List<IlsTag>();
    }

    public class IlsTag
    {
        public Guid IlsId { get; set; }
        public Guid TagId { get; set; }

        public InteractiveLearningSpace? Ils { get; set; }
        public CurriculumTag? Tag { get; set; }
    }
}
