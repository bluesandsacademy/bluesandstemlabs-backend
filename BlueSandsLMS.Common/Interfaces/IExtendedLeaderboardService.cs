using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IExtendedLeaderboardService
    {
        Task<GlobalLeaderboardResponse<StudentRankDto>> GetGlobalStudentsAsync(string metric = "quiz", string period = "all", int top = 50);
        Task<GlobalLeaderboardResponse<TeacherRankDto>> GetGlobalTeachersAsync(string metric = "quiz", string period = "all", int top = 50);
        Task<GlobalLeaderboardResponse<SchoolRankDto>> GetGlobalSchoolsAsync(string metric = "quiz", string period = "all", int top = 50);
    }
}
