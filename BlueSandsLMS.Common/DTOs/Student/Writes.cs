namespace BlueSandsLMS.Common.DTOs.Student
{
    public record StartExperimentRequest(string ExperimentName, string Subject, string Mode, Guid? ClassroomId);
    public record StartExperimentResponse(Guid LaunchId);

    public record SaveExperimentProgressRequest(int LastStep);
    public record CompleteExperimentRequest();

    public record SubmitQuizQuestion(string Id, string Answer, string? Correct);

    public record SubmitQuizRequest(
        string Subject,
        string QuizCode,
        string Type,                // "pre" | "post"
        Guid? ExperimentLaunchId,
        List<SubmitQuizQuestion> Questions);

    public record SubmitQuizResponse(Guid AttemptId, double ScorePercent, bool Passed);
}
