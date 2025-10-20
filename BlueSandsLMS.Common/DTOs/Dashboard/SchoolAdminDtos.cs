namespace BlueSandsLMS.Common.DTOs.Dashboard
{
    public record SchoolOverviewDto(
        TotalsDto Totals,
        SubscriptionCardDto Subscription,
        BillingCardDto Billing,
        LicenseUtilizationDto License,
        VerificationDto Verification,
        Usage7dDto LoginFrequency7d);

    public record TotalsDto(int ActiveTeachers, int ActiveStudents, int Experiments, int Quizzes, int NewRegistrations30d);

    public record SubscriptionCardDto(bool IsActive, string Tier, int Seats, DateTimeOffset? EndsAt, int DaysRemaining);

    // Amounts in KOBO (aligns with Paystack)
    public record BillingCardDto(long? LastPaymentAmount, DateTimeOffset? LastPaymentAt, string Status, string? Promo);

    public record LicenseUtilizationDto(int Allocated, int Used, double Percent);
    public record VerificationDto(int Verified, int Unverified, double RatePercent);
    public record Usage7dDto(IReadOnlyList<int> DailyActiveUsers);

    public record TrendsDto(TrendSeries Series);
    public record TrendSeries(
        IReadOnlyList<DateCount> DailyNewUsers,
        IReadOnlyList<DateAmount> DailyPayments,
        IReadOnlyList<DateCount> DailyExperiments,
        IReadOnlyList<DateCount> DailyAssignments);

    public record DateCount(DateOnly Date, int Count);
    public record DateAmount(DateOnly Date, long Amount);

    public record PerformanceDto(
        double OverallAverageScore,
        double PassRatePercent,
        IReadOnlyList<SubjectScore> SubjectTrends,
        IReadOnlyList<ClassScore> ClassAverages);

    public record SubjectScore(string Subject, double Average, int Samples);
    public record ClassScore(Guid ClassroomId, string ClassName, double Average, int Samples);

    public record TeacherActivityDto(
        IReadOnlyList<TeacherAssignments> AssignmentsCreated,
        TimeSpan? AvgFeedbackTurnaround,
        IReadOnlyList<TeacherEngagement> EngagementScores);

    public record TeacherAssignments(Guid TeacherId, string TeacherName, int Assignments);
    public record TeacherEngagement(Guid TeacherId, string TeacherName, int Score);

    public record ExperimentsCoursesDto(
        int ExperimentsTotal,
        IReadOnlyList<ClassCompletionRate> CompletionRates,
        double ResourceUsagePercent,
        IReadOnlyList<ResourcePopularity> CoursePopularity);

    public record ClassCompletionRate(Guid ClassroomId, string ClassName, double CompletionPercent, int Participants);
    public record ResourcePopularity(string CourseOrModule, int Views);

    public record SystemMetricsDto(
        IReadOnlyList<HourCount> PeakUsageTimes,
        IReadOnlyList<NameCount> DeviceBreakdown,
        IReadOnlyList<NameCount> BrowserBreakdown,
        IReadOnlyList<DateCount> DowntimeOrErrorEvents);

    public record HourCount(int Hour, int Count);
    public record NameCount(string Name, int Count);

    public record LeaderboardDto(
        IReadOnlyList<StudentRank> TopStudents,
        IReadOnlyList<TeacherRank> TopTeachers,
        IReadOnlyList<SchoolRank>? RegionalCompare);

    public record StudentRank(Guid UserId, string Name, double Score, int Rank);
    public record TeacherRank(Guid UserId, string Name, int Activities, int Rank);
    public record SchoolRank(Guid SchoolId, string SchoolName, double Score, int Rank);

    public record BillingDto(SubscriptionCardDto Subscription, IReadOnlyList<PaymentRow> RecentPayments);
    public record PaymentRow(long Id, long Amount, string Status, DateTimeOffset? PaidAt, string? Reference, string? Promo);

    public record CreateUserRequest(string FullName, string Email, string Role, Guid? ClassroomId);
    public record BulkUploadResult(int Created, int Updated, int Failed, IReadOnlyList<string> Errors);
}
