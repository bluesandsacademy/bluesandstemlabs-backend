using System;
using System.Collections.Generic;

namespace BlueSandsLMS.Common.DTOs.Dashboard
{
    public record SchoolOverviewDto(
        Guid SchoolId,
        string SchoolName,
        int TotalStudents,
        int TotalTeachers,
        int ActiveClasses,
        int ExperimentsRunThisTerm,
        int ExperimentsRunAllTime,
        double AvgStudentCompletionRate,
        double AvgStudentScore,
        int WeeklyActiveUsers,
        int MonthlyActiveUsers,
        int TotalIlsCreated
    );

    public record LeaderboardEntry(
        Guid StudentId,
        string StudentName,
        string? ClassName,
        int Points,
        int Rank
    );

    public record ClassroomSummary(
        Guid ClassroomId,
        string ClassroomName,
        int StudentCount,
        int CompletedExperiments,
        double AvgScore,
        int WeeklyActive
    );

    public record ClassroomDetail(
        Guid ClassroomId,
        string ClassroomName,
        IReadOnlyList<StudentRow> Students
    );

    public record StudentRow(
        Guid StudentId,
        string StudentName,
        string? Email,
        int Completed,
        double AvgScore,
        DateTimeOffset? LastActiveAt
    );

    public record TeacherSummary(
        Guid TeacherId,
        string TeacherName,
        string? Email,
        int ClassesHandled,
        int ExperimentsAssigned,
        double AvgClassCompletionRate,
        DateTimeOffset? LastActiveAt
    );

    public record ExperimentUsage(
        Guid ExperimentId,
        string ExperimentTitle,
        int Runs,
        double AvgScore,
        TimeSpan AvgDuration
    );

    public record SchoolRank(Guid SchoolId, string SchoolName, double Score, int Rank);

    public record BillingDto(SubscriptionCardDto Subscription, IReadOnlyList<PaymentRow> RecentPayments)
    {

        public IReadOnlyList<BlueSandsLMS.Common.DTOs.TierSummaryDto> AvailableTiers { get; init; }
            = Array.Empty<BlueSandsLMS.Common.DTOs.TierSummaryDto>();
    }


    public record PaymentRow(
        Guid Id,
        long Amount,
        string Status,
        DateTimeOffset? PaidAt,
        string? Reference,
        string? Promo
    )
    {

        public string Currency { get; init; } = "NGN";
        public string? Method { get; init; }
    }

    public record SubscriptionCardDto(
        string PlanName,
        string Status,
        DateTimeOffset? StartDate,
        DateTimeOffset? EndDate,
        int Seats,
        int SeatsUsed,
        string? RenewalMode
    );

    public record CreateUserRequest(string FullName, string Email, string Role, Guid? ClassroomId);

    public record BulkUploadResult(int Created, int Updated, int Failed, IReadOnlyList<string> Errors);
}
