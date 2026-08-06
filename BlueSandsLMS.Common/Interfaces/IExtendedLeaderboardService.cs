using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IExtendedLeaderboardService
    {
        Task<List<StudentRankDto>> GetGlobalStudentsAsync(string metric = "quiz", string period = "all", int top = 50);
        Task<List<TeacherRankDto>> GetGlobalTeachersAsync(string metric = "quiz", string period = "all", int top = 50);
        Task<List<SchoolRankDto>> GetGlobalSchoolsAsync(string metric = "quiz", string period = "all", int top = 50);
    }
}
