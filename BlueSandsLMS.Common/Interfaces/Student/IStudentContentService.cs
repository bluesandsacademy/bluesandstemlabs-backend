using BlueSandsLMS.Common.DTOs.Student;

namespace BlueSandsLMS.Common.Interfaces.Student
{
    public interface IStudentContentService
    {
        Task<IReadOnlyList<SubjectTileDto>> GetSubjectsAsync(Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<LessonDto>> GetLessonsAsync(Guid userId, string subjectCode, CancellationToken ct = default);
        Task<LessonCompleteDto> CompleteLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);
        Task<IReadOnlyList<CertificateDto>> GetCertificatesAsync(Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(Guid userId, CancellationToken ct = default);
    }
}
