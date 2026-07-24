using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Core.Entities
{
    public class StudentIlsSession
    {
        [Key] public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid IlsId { get; set; }
        public int CurrentStep { get; set; } = 1;
        public bool ReflectionSubmitted { get; set; }
        public bool BadgeAwarded { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? LastSimulationStateJson { get; set; }
        public string? UploadedFileUrl { get; set; }

        public User? Student { get; set; }
        public InteractiveLearningSpace? Ils { get; set; }
        public SessionPoll? Poll { get; set; }
        public SessionHypothesis? Hypothesis { get; set; }
        public SessionReflection? Reflection { get; set; }
        public SessionAssessment? Assessment { get; set; }
        public SessionOrientation? Orientation { get; set; }
        public SessionRealWorld? RealWorld { get; set; }
        public ICollection<SessionTrial> Trials { get; set; } = new List<SessionTrial>();
        public ICollection<IlsDiscussionMessage> DiscussionMessages { get; set; } = new List<IlsDiscussionMessage>();
    }

    public class SessionPoll
    {
        [Key] public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public int OptionIndex { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)] public string? QuizTitle { get; set; }
        public int? TimeSpentSeconds { get; set; }
        public string? AnswersJson { get; set; }
        public int? Score { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }

        public StudentIlsSession? Session { get; set; }
    }

    public class SessionOrientation
    {
        [Key] public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        [MaxLength(8000)] public string EngagementAnswer { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public StudentIlsSession? Session { get; set; }
    }

    public class SessionRealWorld
    {
        [Key] public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        [MaxLength(8000)] public string Note { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public StudentIlsSession? Session { get; set; }
    }

    public class SessionHypothesis
    {
        [Key] public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        [MaxLength(8000)] public string Text { get; set; } = string.Empty;
        [MaxLength(20)] public string InputMethod { get; set; } = "text";
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public StudentIlsSession? Session { get; set; }
    }

    public class SessionTrial
    {
        [Key] public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string VariablesJson { get; set; } = "{}";
        public string? ObservationText { get; set; }
        public string ResultJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public StudentIlsSession? Session { get; set; }
    }

    public class SessionReflection
    {
        [Key] public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        [MaxLength(8000)] public string Text { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public StudentIlsSession? Session { get; set; }
    }

    public class SessionAssessment
    {
        [Key] public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public string AnswersJson { get; set; } = "[]";
        public decimal Score { get; set; }
        [MaxLength(4000)] public string Feedback { get; set; } = string.Empty;
        public bool IsAutoSubmitted { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public StudentIlsSession? Session { get; set; }
    }

    public class IlsDiscussionMessage
    {
        [Key] public Guid Id { get; set; }
        public Guid IlsId { get; set; }
        public Guid SessionId { get; set; }
        public Guid AuthorId { get; set; }
        [MaxLength(8000)] public string Text { get; set; } = string.Empty;
        public bool Flagged { get; set; }
        [MaxLength(500)] public string? FlagReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FlaggedAt { get; set; }
        public Guid? FlaggedBy { get; set; }

        public InteractiveLearningSpace? Ils { get; set; }
        public StudentIlsSession? Session { get; set; }
        public User? Author { get; set; }
    }
}
