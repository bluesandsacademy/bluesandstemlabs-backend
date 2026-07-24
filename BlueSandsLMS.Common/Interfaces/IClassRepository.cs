using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;  

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IClassRepository
    {
        Task<Guid> CreateAsync(Guid schoolId, Guid teacherId, string name, string subject);
        Task UpdateAsync(Guid classId, string name, string subject);
        Task DeleteAsync(Guid classId);

        Task<bool> UserIsTeacherAsync(Guid classId, Guid userId);
        Task EnrollByEmailAsync(Guid classId, string email);
        Task BulkEnrollAsync(Guid classId, IEnumerable<string> emails);


        Task<(string code, DateTime? expiresAt)> RotateInviteCodeAsync(Guid classId, int expireDays);
        Task<Guid?> GetClassroomIdByInviteAsync(string code);
        Task JoinByCodeAsync(Guid userId, string code);
        Task<List<ClassSummaryDto>> GetMyClassesAsync(Guid userId);

        Task<List<ClassSummaryDto>> GetClassesBySchoolIdAsync(Guid schoolId);
    }
}
