using BlueSandsLMS.Common.DTOs.Dashboard;

namespace BlueSandsLMS.Common.Interfaces.Student
{
    public interface IStudentLeaderboardService
    {
        Task<LeaderboardDto> GetAsync(Guid userId, string scope, int take, CancellationToken ct);
    }
}
