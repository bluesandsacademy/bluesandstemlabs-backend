using System;
using System.Collections.Generic;

namespace BlueSandsLMS.Common.DTOs
{

    public record CreateClassDto(string Name, string Subject);
    public record UpdateClassDto(string Name, string Subject);
    public record EnrollByEmailDto(string Email);
    public record BulkEnrollDto(List<string> Emails);


    public enum AssignmentType { Lab = 0, Quiz = 1 }
    public record CreateAssignmentDto(Guid ClassroomId, string Title, AssignmentType Type, string ResourceCode, DateTime? DueAt);
    public record UpdateAssignmentDto(string Title, AssignmentType Type, string ResourceCode, DateTime? DueAt);


    public record GradeSubmissionDto(decimal Score0to1, string? Feedback);


    public record CreateAnnouncementDto(Guid ClassroomId, string Title, string Body);
    public record UpdateAnnouncementDto(string Title, string Body);


    public record AddParentLinkDto(Guid StudentId, string ParentEmail, bool IsPrimary);


    public record ToGradeItemDto(
        Guid SubmissionId,
        Guid AssignmentId,
        Guid StudentId,
        DateTime? SubmittedAt,
        decimal? Score0to1,
        int Status
    );

    public record ParentLinkDto(
        Guid Id,
        string ParentEmail,
        bool IsPrimary,
        DateTime CreatedAt
    );

    public record RotateInviteCodeDto(int ExpireDays = 14);
    public record JoinByCodeDto(string Code);

    public enum ClassRoleDto { Student = 0, Teacher = 1 }

    public record ClassSummaryDto(
        Guid Id,
        string Name,
        string Subject,
        ClassRoleDto MyRole,
        int Students,
        DateTime CreatedAt,
        string? InviteCode,
        DateTime? InviteCodeExpiresAt
    );
}
