using System;
using System.Collections.Generic;

namespace BlueSandsLMS.Common.DTOs.Admin
{

    
    public sealed record GlobalAdminTotalsDto(
    int TotalUsers,
    int ActiveUsers30d,
    int TotalSchools,
    long TotalExperimentAttempts,
    long TotalQuizAttempts,
    long TotalLabTimeMinutes,
    decimal TotalRevenueNGN,
    int totalTeachers,
    int totalStudent,
    int ActiveSubscriptions,


    int TotalPayments,
    int TotalStemCourses,
    double TotalQuizScores,
    int TotalSubscribedUsers,
    int MaleUsers,
    int FemaleUsers,
    int OfflineUsers,
    int TotalIls,

    DateTime GeneratedAtUtc
);



    
    public record SeriesPoint(DateTime T, long V);
    
    public record GrowthSeriesDto(string Metric, string Period, SeriesPoint[] Points);


    
    public record GeoRow(string Key, int Schools, int Users, long Experiments, long Quizzes);
    
    public record GeoAdvancedDto(string Scope, string? Country, string? State, GeoRow[] Rows, DateTime GeneratedAtUtc);
    
    public record GeoUsageRow(string Country, int Schools, int Users, long Experiments, long QuizAttempts);
    
    public record GeoUsageDto(GeoUsageRow[] Rows, DateTime GeneratedAtUtc);


    
    public record LabeledValue(string Label, double Value);
    
    public record HourBucket(int Hour, long Count);
    
    public record GlobalAiInsightsDto(
        DateTime GeneratedAtUtc,
        IReadOnlyList<LabeledValue> TopExperiments,
        IReadOnlyList<LabeledValue> TopSubjects,
        IReadOnlyList<HourBucket> PeakUsage,
        double AvgQuizScorePercent,
        long TotalActiveUsers30d
    );


    
    public record GlobalAdminUserRowDto(
        Guid Id,
        string FullName,
        string Email,
        string RoleName,
        Guid? SchoolId,
        string? SchoolName,
        bool IsActive,
        bool IsEmailVerified,
        DateTime DateCreated,
        DateTime? LastLogin
    );

    public record UserQuery(string? Q, string? Role, string? Status, int Page = 1, int PageSize = 20);
    
    public record SetUserRoleRequest(Guid RoleId);
    
    public record SetUserActiveRequest(bool IsActive);
    
    public record ResetPasswordResponse(string TemporaryPassword, DateTime ResetAtUtc);


    
    public record PaymentRowDto(
        Guid Id,
        Guid? SchoolId,
        string SchoolName,
        string Currency,
        decimal Subtotal,
        decimal Vat,
        decimal Total,
        string Status,
        string Provider,
        string Reference,
        DateTime DateCreated
    );

    public record SubscriptionRowDto(
        Guid Id,
        Guid? SchoolId,
        string SchoolName,
        int StudentsCovered,
        decimal PricePerStudent,
        DateTime StartsAt,
        DateTime? EndsAt,
        bool Active,
        string? LastPaymentReference
    );

    public record RevenueBreakdownDto(
        decimal TotalPaidNGN, 
        int PaymentsPaid, 
        int SubscriptionsActive, 
        DateTime GeneratedAtUtc);


    
    public record SchoolRankDto(Guid SchoolId, string SchoolName, double Score, int Rank);
    
    public record TeacherRankDto(Guid UserId, string Name, double Score, int Rank);
    
    //public record StudentRankDto(Guid UserId, string Name, double Score, int Rank);
    public record StudentRankDto(string Name, string School, string Country, string Experiments,int Points,int Average);

    public record StudentRank(Guid UserId, string Name, string? SchoolName, double Score, int Rank);
    
    public record TeacherRank(Guid UserId, string Name, string? SchoolName, double Score, int Rank);

    public record GlobalLeaderboardResponse<TEntry>(
        string Entity,
        string Metric,
        string Period,
        DateTime GeneratedAtUtc,
        IReadOnlyList<TEntry> Entries
    );


    
    public class TicketQuery
    {
        public string? Q { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public Guid? SchoolId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public record TicketRowDto(
        Guid Id,
        string Subject,
        string Preview,
        string Status,
        string Priority,
        Guid? SchoolId,
        string? School,
        Guid CreatedByUserId,
        string CreatedByName,
        Guid? AssignedToUserId,
        string? AssignedToName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string? TagsCsv
    );

    public record TicketDetailDto(
        Guid Id,
        string Subject,
        string Body,
        string Status,
        string Priority,
        string Source,
        Guid? SchoolId,
        string? School,
        Guid CreatedByUserId,
        string CreatedByName,
        Guid? AssignedToUserId,
        string? AssignedToName,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? ClosedAt,
        string? TagsCsv,
        IReadOnlyList<TicketCommentDto> Comments
    );

    public record TicketCommentDto(
        Guid Id,
        Guid UserId,
        string UserName,
        string Body,
        bool IsPrivate,
        DateTime CreatedAt
    );

    public class CreateTicketRequest
    {
        public Guid? SchoolId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public string Priority { get; set; } = "Medium";
        public string Source { get; set; } = "System";
        public string? TagsCsv { get; set; }
    }

    public class UpdateTicketRequest
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? TagsCsv { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }

    public class AddTicketCommentRequest
    {
        public Guid UserId { get; set; }
        public string Body { get; set; } = "";
        public bool IsPrivate { get; set; }
    }


    
    public record SupportMessageDto(
        Guid Id,
        Guid? FromUserId,
        string? FromEmail,
        string Channel,
        string Body,
        DateTime At,
        Guid? SchoolId
    );

    public record SupportOverviewDto(
        int MessagesLast7d,
        int MessagesOpen,
        int DistinctSchoolsLast7d
    );


    
    public record GlobalExportRequest(string Type, string? Period = "month");


    
    public record PagedResult<T>(int Page, int PageSize, int Total, IReadOnlyList<T> Items);
}