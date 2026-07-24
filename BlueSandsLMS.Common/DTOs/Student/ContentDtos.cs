using System;

namespace BlueSandsLMS.Common.DTOs.Student
{

    public record SubjectTileDto(
        string Code,
        string Name,
        int Lessons,
        int Completed,
        int ProgressPercent);


    public record LessonDto(
        Guid Id,
        string Title,
        string? Summary,
        int DurationMin,
        bool Completed);


    public record LessonCompleteDto(
        Guid LessonId,
        DateTime CompletedAt);


    public record CertificateDto(
        Guid Id,
        string Title,
        string SubjectCode,
        DateTime IssuedAt,
        string IssuedBy);


    public record RecommendationDto(
        string SubjectCode,
        string Topic,
        string Reason);
}
