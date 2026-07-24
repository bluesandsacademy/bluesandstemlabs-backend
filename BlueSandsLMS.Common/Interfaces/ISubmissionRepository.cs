using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface ISubmissionRepository
    {

        Task<Guid?> GetClassroomIdByAssignmentAsync(Guid assignmentId);
        Task<bool> IsStudentEnrolledAsync(Guid assignmentId, Guid studentId);
        Task<bool> IsTeacherOfAssignmentAsync(Guid assignmentId, Guid teacherId);


        Task<Guid> SubmitAsync(Guid assignmentId, Guid studentId, SubmitWorkDto dto);
        Task ResubmitAsync(Guid submissionId, Guid studentId, ResubmitWorkDto dto);
        Task<SubmissionDetailDto?> GetMineAsync(Guid assignmentId, Guid studentId);


        Task GradeAsync(Guid submissionId, Guid teacherId, decimal score0to1, string? feedback);


        Task<List<SubmissionSummaryDto>> ListByAssignmentAsync(Guid assignmentId, int skip, int take);
    }
}
