using System;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces.Student
{
    public interface IStudentLeaderboardService
    {
        Task<LeaderboardDto> GetLeaderboardAsync(Guid studentId, string scope, CancellationToken ct);
    }
}