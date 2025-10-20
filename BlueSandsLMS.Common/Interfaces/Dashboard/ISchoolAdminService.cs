using BlueSandsLMS.Common.DTOs.Dashboard;

namespace BlueSandsLMS.Common.Interfaces.Dashboard
{
    public interface ISchoolAdminService
    {
        Task<SchoolOverviewDto>     GetOverviewAsync(Guid schoolId, CancellationToken ct);
        Task<TrendsDto>             GetTrendsAsync(Guid schoolId, int days, CancellationToken ct);
        Task<PerformanceDto>        GetPerformanceAsync(Guid schoolId, DateOnly? since, DateOnly? until, CancellationToken ct);
        Task<TeacherActivityDto>    GetTeacherActivityAsync(Guid schoolId, int days, CancellationToken ct);
        Task<ExperimentsCoursesDto> GetExperimentsAndCoursesAsync(Guid schoolId, int days, CancellationToken ct);
        Task<SystemMetricsDto>      GetSystemMetricsAsync(Guid schoolId, int days, CancellationToken ct);
        Task<LeaderboardDto>        GetLeaderboardAsync(Guid schoolId, int take, CancellationToken ct);
        Task<BillingDto>            GetBillingAsync(Guid schoolId, CancellationToken ct);

        Task<Guid>                  CreateUserAsync(Guid schoolId, CreateUserRequest req, CancellationToken ct);
        Task<BulkUploadResult>      BulkUploadUsersCsvAsync(Guid schoolId, byte[] csvBytes, CancellationToken ct);
        Task                        AssignRoleAsync(Guid userId, string role, CancellationToken ct);
    }
}
