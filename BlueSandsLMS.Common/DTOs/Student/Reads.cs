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
        IReadOnlyList<string> Recommendations)
    {

        public int QuizzesAttempted { get; init; }
        public int QuizzesPassed { get; init; }
        public DateTime? MostRecentAttemptDate { get; init; }
    }

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
        DateTime? EndedAt)
    {

        public bool Completed { get; init; }
        public int DurationMinutes { get; init; }
    }

    public record StudentBadgeDto(string Code, string Name, string? Description, DateTime AwardedAt);
    public record StudentLeaderboardEntry(Guid UserId, string Name, double ScorePercent);


    public record StudentAssessmentSummaryDto(
        int QuizzesAttempted,
        int QuizzesPassed,
        double AvgQuizScorePercent,
        DateTime? MostRecentQuizDate,
        int IlsAssessmentsCompleted,
        double AvgIlsScorePercent);
}
