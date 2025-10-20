using System.Security.Claims;
using BlueSandsLMS.Common.DTOs.Student;
using BlueSandsLMS.Common.Interfaces.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/student/v1/content")]
    [Authorize(Roles = "Student,SchoolAdmin")]
    public sealed class StudentV1ContentController : ControllerBase
    {
        private readonly IStudentContentService _svc;
        public StudentV1ContentController(IStudentContentService svc) => _svc = svc;
        private Guid Me() => Guid.Parse(User.FindFirst("sub")!.Value);

        [HttpGet("subjects")]
        public async Task<ActionResult<IReadOnlyList<SubjectTileDto>>> Subjects(CancellationToken ct)
            => Ok(await _svc.GetSubjectsAsync(Me(), ct));

        [HttpGet("subjects/{subjectCode}/lessons")]
        public async Task<ActionResult<IReadOnlyList<LessonDto>>> Lessons([FromRoute] string subjectCode, CancellationToken ct)
            => Ok(await _svc.GetLessonsAsync(Me(), subjectCode, ct));

        [HttpPost("lessons/{lessonId:guid}/complete")]
        public async Task<ActionResult<LessonCompleteDto>> Complete([FromRoute] Guid lessonId, CancellationToken ct)
            => Ok(await _svc.CompleteLessonAsync(Me(), lessonId, ct));

        [HttpGet("certificates")]
        public async Task<ActionResult<IReadOnlyList<CertificateDto>>> Certificates(CancellationToken ct)
            => Ok(await _svc.GetCertificatesAsync(Me(), ct));

        [HttpGet("recommendations")]
        public async Task<ActionResult<IReadOnlyList<RecommendationDto>>> Recommendations(CancellationToken ct)
            => Ok(await _svc.GetRecommendationsAsync(Me(), ct));
    }
}