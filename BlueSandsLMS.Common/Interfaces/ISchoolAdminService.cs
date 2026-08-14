using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Common.Interfaces
{
    public interface ISchoolAdminService
    {
        Task<UpsertResultDto> UpsertTeacherAsync(UpsertTeacherDto dto);
        Task<IReadOnlyList<UpsertResultDto>> BulkUpsertTeachersAsync(Guid adminUserId, Guid schoolId, BulkUpsertTeachersDto dto);

        Task<UpsertResultDto> UpsertStudentAsync(UpsertStudentDto dto);
        Task<IReadOnlyList<UpsertResultDto>> BulkUpsertStudentsAsync(Guid adminUserId, Guid schoolId, BulkUpsertStudentsDto dto);
        Task AssignRoleAsync(Guid userId, string role, CancellationToken ct);
       // Task<UpsertResultDto> RegisterOrAssignUserToSchoolAsync(
       //string Gender, string fullName, string? phone, string? country, string roleName, CancellationToken ct);
    }
}
