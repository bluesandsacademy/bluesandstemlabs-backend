namespace BlueSandsLMS.Common.DTOs
{
    public sealed class QuizQuestion
    {
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    public sealed class QuizData
    {
        public string QuizTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Points { get; set; } = string.Empty;
        public List<QuizQuestion> Questions { get; set; } = new();
    }

    public sealed class CreateIlsRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public string Score { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;

        public string SimulationId { get; set; } = string.Empty;

        public string SimulationSource { get; set; } = "phet";

        public string? PraxiLabsExperimentId { get; set; }
        public QuizData PreSimAssessment { get; set; } = new();
        public QuizData PostSimAssessment { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string IntroductionMessage { get; set; } = string.Empty;
        public string EngagementQuestion { get; set; } = string.Empty;
        public string HypothesisQuestion { get; set; } = string.Empty;
        public List<string> ExperimentProcedures { get; set; } = new();
        public string DiscussionPrompt { get; set; } = string.Empty;
        public List<string> RealWorldApplications { get; set; } = new();
        public List<string> RelatedCareers { get; set; } = new();
        public string RealWorldTask { get; set; } = string.Empty;
    }

    public sealed class UpdateIlsRequest
    {
        public string? Title { get; set; }
        public string? Objective { get; set; }
        public string? Score { get; set; }
        public string? Duration { get; set; }
        public string? SimulationId { get; set; }
        public string? SimulationSource { get; set; }
        public string? PraxiLabsExperimentId { get; set; }
        public List<string>? Tags { get; set; }
        public QuizData? PreSimAssessment { get; set; }
        public QuizData? PostSimAssessment { get; set; }
        public string? IntroductionMessage { get; set; }
        public string? EngagementQuestion { get; set; }
        public string? HypothesisQuestion { get; set; }
        public List<string>? ExperimentProcedures { get; set; }
        public string? DiscussionPrompt { get; set; }
        public List<string>? RealWorldApplications { get; set; }
        public List<string>? RelatedCareers { get; set; }
        public string? RealWorldTask { get; set; }
    }

    public sealed class AssignIlsRequest
    {
        public Guid ClassroomId { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public sealed class IlsTagItemDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }

    public sealed class IlsDetailsDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public Guid? SimulationId { get; set; }
        public string SimulationSource { get; set; } = "phet";
        public string? PraxiLabsExperimentId { get; set; }
        public string Status { get; set; } = "draft";
        public Guid CreatedBy { get; set; }
        public bool IsSharedWithSchool { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<IlsTagItemDto> Tags { get; set; } = new();
    }

    public sealed class IlsFormDataDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public string Score { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string SimulationId { get; set; } = string.Empty;
        public string SimulationSource { get; set; } = "phet";
        public string? PraxiLabsExperimentId { get; set; }
        public QuizData PreSimAssessment { get; set; } = new();
        public QuizData PostSimAssessment { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string IntroductionMessage { get; set; } = string.Empty;
        public string EngagementQuestion { get; set; } = string.Empty;
        public string HypothesisQuestion { get; set; } = string.Empty;
        public List<string> ExperimentProcedures { get; set; } = new();
        public string DiscussionPrompt { get; set; } = string.Empty;
        public List<string> RealWorldApplications { get; set; } = new();
        public List<string> RelatedCareers { get; set; } = new();
        public string RealWorldTask { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
        public Guid CreatedBy { get; set; }
        public bool IsSharedWithSchool { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateTagRequest
    {
        public string Label { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }

    public sealed class TagDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }

    public sealed class SimulationListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PreviewUrl { get; set; }
    }
}
