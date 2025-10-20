using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IDashboardService
    {
        Task<StudentDashboardDto> GetStudentAsync(Guid userId);
        Task<TeacherDashboardDto> GetTeacherAsync(Guid teacherId);
        Task<SchoolAdminDashboardDto> GetSchoolAdminAsync(Guid adminUserId, Guid schoolId);
        Task<GlobalDashboardDto> GetGlobalAsync();
    }
}
