namespace BlueSandsLMS.Common.DTOs
{
    public sealed class CreateSessionRequest
    {
        public Guid StudentId { get; set; }
        public Guid IlsId { get; set; }
    }

    public sealed class PollSubmissionRequest
    {
        public int OptionIndex { get; set; }

        public string? QuizTitle { get; set; }
        public int? TimeSpentSeconds { get; set; }
        public List<PollAnswerRequest>? Answers { get; set; }
        public int? Score { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
    }

    public sealed class PollAnswerRequest
    {
        public int QuestionIndex { get; set; }
        public int OptionIndex { get; set; }
        public bool IsCorrect { get; set; }
    }

    public sealed class OrientationSubmissionRequest
    {
        public string EngagementAnswer { get; set; } = string.Empty;
    }

    public sealed class RealWorldSubmissionRequest
    {
        public string Note { get; set; } = string.Empty;
    }

    public sealed class HypothesisSubmissionRequest
    {
        public string Text { get; set; } = string.Empty;
        public string InputMethod { get; set; } = "text";
    }

    public sealed class ExperimentSubmissionRequest
    {
        public SimulationVariables Variables { get; set; } = new();
        public string? ObservationText { get; set; }
    }

    public sealed class ReflectionSubmissionRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public sealed class AssessmentAnswerRequest
    {
        public string QId { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public sealed class AssessmentSubmissionRequest
    {
        public List<AssessmentAnswerRequest> Answers { get; set; } = new();

        public List<PostSimAnswerRequest>? PostSimAnswers { get; set; }
        public int? PostSimScore { get; set; }
        public int? PostSimTotal { get; set; }
    }

    public sealed class PostSimAnswerRequest
    {
        public int QuestionIndex { get; set; }
        public string SelectedAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public sealed class DiscussionPostRequest
    {
        public Guid SessionId { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public sealed class DiscussionFlagRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class SimulationVariables
    {
        public double Density { get; set; }
        public double Volume { get; set; }
        public double Temp { get; set; }
    }

    public sealed class GraphPoint
    {
        public int X { get; set; }
        public double Y { get; set; }
    }

    public sealed class SocketVariablesFrame
    {
        public string Action { get; set; } = string.Empty;
        public SimulationVariables Variables { get; set; } = new();
    }
}
