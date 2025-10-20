using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface IAssignmentRepository
    {
        Task<Guid> CreateAsync(Guid classroomId, string title, int type, string resourceCode, DateTime? dueAt, Guid creatorUserId);
        Task UpdateAsync(Guid assignmentId, string title, int type, string resourceCode, DateTime? dueAt);
        Task DeleteAsync(Guid assignmentId);

        Task<List<ToGradeItemDto>> GetToGradeAsync(Guid classroomId, int take, int skip);

        // Ownership/guard helpers
        Task<Guid?> GetClassroomIdAsync(Guid assignmentId);
    }
}
