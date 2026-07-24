using System;

namespace BlueSandsLMS.Common.DTOs
{

    public record SubmitWorkDto(
        Guid AssignmentId,
        string? AttachmentUrl,
        string? AttachmentName,
        long? AttachmentSizeBytes,
        string? AttachmentContentType,
        string? TextAnswer
    );


    public record ResubmitWorkDto(
        string? AttachmentUrl,
        string? AttachmentName,
        long? AttachmentSizeBytes,
        string? AttachmentContentType,
        string? TextAnswer
    );


    public record SubmissionSummaryDto(
        Guid SubmissionId,
        Guid AssignmentId,
        Guid StudentId,
        string StudentName,
        string StudentEmail,
        int Status,
        decimal? Score0to1,
        DateTime? SubmittedAt,
        DateTime? GradedAt
    );

    public record SubmissionDetailDto(
        Guid SubmissionId,
        Guid AssignmentId,
        Guid StudentId,
        int Status,
        decimal? Score0to1,
        DateTime? SubmittedAt,
        DateTime? GradedAt,
        string? Feedback,
        Guid? GraderUserId,
        string? AttachmentUrl,
        string? AttachmentName,
        long? AttachmentSizeBytes,
        string? AttachmentContentType
    );
}
