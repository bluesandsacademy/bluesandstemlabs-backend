using System;

namespace BlueSandsLMS.Common.DTOs.Student
{
    // === Subjects tile on the student home ===
    // Matches: new SubjectTileDto(code, name, lessons, completed, progressPercent)
    public record SubjectTileDto(
        string Code,
        string Name,
        int Lessons,
        int Completed,
        int ProgressPercent);

    // === Lessons list for a subject ===
    // Matches: new LessonDto(id, title, summary, durationMin, completed)
    public record LessonDto(
        Guid Id,
        string Title,
        string? Summary,
        int DurationMin,
        bool Completed);

    // === Lesson completion result ===
    // Matches: new LessonCompleteDto(lessonId, completedAt)
    public record LessonCompleteDto(
        Guid LessonId,
        DateTime CompletedAt);

    // === Certificates ===
    // Matches: new CertificateDto(id, title, subjectCode, issuedAt, issuedBy)
    public record CertificateDto(
        Guid Id,
        string Title,
        string SubjectCode,
        DateTime IssuedAt,
        string IssuedBy);

    // === Recommendations ===
    // Matches: new RecommendationDto(subjectCode, topic, reason)
    public record RecommendationDto(
        string SubjectCode,
        string Topic,
        string Reason);
}
