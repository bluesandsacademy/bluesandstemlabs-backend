using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Student;

namespace BlueSandsLMS.Common.Interfaces.Student
{
    public interface IStudentDashboardService
    {
        Task<StudentOverviewDto> GetOverviewAsync(Guid userId, CancellationToken ct = default);

        Task<IReadOnlyList<StudentAttemptDto>> GetRecentQuizAttemptsAsync(
            Guid userId,
            int take = 10,
            CancellationToken ct = default);

        Task<IReadOnlyList<StudentExperimentDto>> GetRecentExperimentsAsync(
            Guid userId,
            int take = 10,
            CancellationToken ct = default);

        Task<IReadOnlyList<StudentBadgeDto>> GetBadgesAsync(
            Guid userId,
            CancellationToken ct = default);

        Task<IReadOnlyList<StudentLeaderboardEntry>> GetLeaderboardAsync(
            Guid userId,
            string scope, // "class" | "school" | "global"
            int take = 20,
            CancellationToken ct = default);
    }
}
