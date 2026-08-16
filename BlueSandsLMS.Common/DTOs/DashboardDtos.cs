namespace BlueSandsLMS.Common.DTOs;



public record MeDto(Guid UserId, string FullName, string Email, string Role, Guid? SchoolId, bool IsVerified);

public record StudentDashboardDto(
    bool IsVerified,
    StudentQuick Quick,
    IEnumerable<SimpleItem> Due,
    IEnumerable<SimpleItem> Recent
);

public record StudentQuick(int ExperimentsCompleted, decimal AvgQuizScore, int Badges, int TimeSpentMins7d, Rank Rank);

public record Rank(int Class, int School, int National);

public record SimpleItem(string Type, string Title, DateTime At, Guid? Id = null);

public record TeacherDashboardDto(
    int Classes,
    int Students,
    int ToGrade,
    int TotalIlsCreated,
    IEnumerable<TopStudent> TopStudents,
    IEnumerable<AtRiskStudent> AtRisk,
    Activity Activity7d
);

public record TopStudent(string Name, decimal Score);

public record AtRiskStudent(string Name, DateTime? LastActive);

public record Activity(int Logins, int Experiments, int Quizzes);

public record SchoolAdminDashboardDto(
    Counts Counts,
    decimal VerificationRate,
    Usage7d Activity7d,
    IEnumerable<string> TopSubjects,
    double LoginFrequency
);

public record Counts(int Teachers, int Students);

public record Usage7d(int ActiveUsers, int Experiments, int Quizzes);

public record GlobalDashboardDto(
    int TotalUsers,
    int TotalSchools,
    int TotalIls,
    int TotalExperiments,
    int TotalQuizAttempts
);


public record DataPoint(DateTime Timestamp, int Value, string Label);

public record TrendsDto(
    IReadOnlyList<DataPoint> ActiveUsers,
    IReadOnlyList<DataPoint> ExperimentsRun,
    IReadOnlyList<DataPoint> AvgScores);

public record PerformanceDto(
    double AvgScore,
    double MedianScore,
    int CompletedExperiments,
    double CompletionRate);

public record TeacherActivityDto(
    Guid TeacherId,
    string TeacherName,
    string? Email,
    int Classes,
    int ExperimentsAssigned,
    int StudentsReached,
    DateTimeOffset? LastActiveAt);

public record ItemCount(Guid Id, string Title, int Count);

public record ExperimentsCoursesDto(
    IReadOnlyList<ItemCount> TopExperiments,
    IReadOnlyList<ItemCount> TopCourses);

public record SystemMetricsDto(
    int TotalSchools,
    int TotalTeachers,
    int TotalStudents,
    int TotalExperiments,
    int MonthlyActiveUsers);

public record LeaderboardEntry(
    Guid UserId,
    string StudentName,
    string? ClassName,
    int Points,
    int Rank);

public record LeaderboardDto(
    IReadOnlyList<LeaderboardEntry> Entries,
    int Total,
    int Page,
    int PageSize);

/// <summary>
/// Represents the complete dataset for the growth chart widget.
/// </summary>
public record GrowthChartResponse(
    string Title,                // e.g., "Growth — users"
    string MetricName,           // e.g., "users"
    IReadOnlyList<DataPoints> DataPoints
);

/// <summary>
/// Represents a single data point on the chart.
/// </summary>
public record DataPoints(
    DateTime Timestamp,          // Date/time for the X-axis (e.g., 2026-07-26)
    int Value,                   // Metric value for the Y-axis (e.g., 43)
    string Label                 // Formatted display string (e.g., "Jul 26")
);