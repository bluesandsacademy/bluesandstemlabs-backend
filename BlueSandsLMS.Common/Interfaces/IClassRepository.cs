using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;


namespace BlueSandsLMS.Common.Interfaces
{
    public interface IClassRepository
    {
        Task<Guid> CreateAsync(Guid schoolId, Guid teacherId, string name, string subject);
        Task UpdateAsync(Guid classId, string name, string subject);
        Task DeleteAsync(Guid classId);

        Task<bool> UserIsTeacherAsync(Guid classId, Guid userId);
        Task EnrollByEmailAsync(Guid classId, string email, ClassRole role = ClassRole.Student);
        Task BulkEnrollAsync(Guid classId, IEnumerable<string> emails, ClassRole role = ClassRole.Student);
        Task TransferEnrollmentAsync(Guid classId, string email, Guid? newClassId = null, ClassRole? role = null);

        Task AttachTeacherAsync(Guid classId, Guid teacherUserId);

        Task<(string code, DateTime? expiresAt)> RotateInviteCodeAsync(Guid classId, int expireDays);
        Task<Guid?> GetClassroomIdByInviteAsync(string code);
        Task JoinByCodeAsync(Guid userId, string code);
        Task<List<ClassSummaryDto>> GetMyClassesAsync(Guid userId);

        Task<List<ClassSummaryDto>> GetClassesBySchoolIdAsync(Guid schoolId);
        Task AttachStudentAsync(Guid classId, Guid studentUserId);
    }
}