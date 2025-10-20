namespace BlueSandsLMS.Common.DTOs.Student
{
    public record StudentOverviewDto(
        int CompletedExperiments,
        int InProgressExperiments,
        double AvgQuizScorePercent,
        int BadgesCount,
        int MinutesInLab7d,
        int RankClass,
        int RankSchool,
        string Greeting,
        IReadOnlyList<string> Recommendations);

    public record StudentAttemptDto(
        Guid AttemptId,
        string Subject,
        string QuizCode,
        double ScorePercent,
        bool Passed,
        DateTime CompletedAt);

    public record StudentExperimentDto(
        Guid LaunchId,
        string Subject,
        string ExperimentName,
        string Mode,
        int LastStep,
        DateTime StartedAt,
        DateTime? EndedAt);

    public record StudentBadgeDto(string Code, string Name, string? Description, DateTime AwardedAt);
    public record StudentLeaderboardEntry(Guid UserId, string Name, double ScorePercent);
}
