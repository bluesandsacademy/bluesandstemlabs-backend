using System;
using System.Collections.Generic;

namespace BlueSandsLMS.Common.DTOs
{
    public record UpsertTeacherDto(string Email, string FullName, string? Phone = null, string? Country = null);
    public record UpsertStudentDto(string Email, string FullName, string? Phone = null, string? Country = null);

    public record BulkUpsertTeachersDto(List<UpsertTeacherDto> Teachers);
    public record BulkUpsertStudentsDto(List<UpsertStudentDto> Students);

    public record UpsertResultDto(string Email, string Action, Guid UserId, string Role, Guid SchoolId);

    public record AssignRoleDto(string Role);
}
