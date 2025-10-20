using System;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface ILeaderboardService
    {
        Task<LeaderboardResponseDto> GetClassAsync(Guid classId, string metric = "quiz", int top = 50);
        Task<LeaderboardResponseDto> GetSchoolAsync(Guid schoolId, string metric = "quiz", int top = 50);
        Task<LeaderboardResponseDto> GetGlobalAsync(string metric = "quiz", int top = 50);
    }
}
