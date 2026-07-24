using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface ISchoolAdminService
    {
        Task<UpsertResultDto> UpsertTeacherAsync(Guid adminUserId, Guid schoolId, UpsertTeacherDto dto);
        Task<IReadOnlyList<UpsertResultDto>> BulkUpsertTeachersAsync(Guid adminUserId, Guid schoolId, BulkUpsertTeachersDto dto);

        Task<UpsertResultDto> UpsertStudentAsync(Guid adminUserId, Guid schoolId, UpsertStudentDto dto);
        Task<IReadOnlyList<UpsertResultDto>> BulkUpsertStudentsAsync(Guid adminUserId, Guid schoolId, BulkUpsertStudentsDto dto);


        Task AssignRoleAsync(Guid userId, string role, CancellationToken ct);
    }
}
