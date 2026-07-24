using BlueSandsLMS.Common.DTOs.Student;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services.Student
{
    public sealed class StudentContentService : IStudentContentService
    {
        private readonly BlueSandsLMSDbContext _db;

        public StudentContentService(BlueSandsLMSDbContext db) => _db = db;

        public async Task<IReadOnlyList<SubjectTileDto>> GetSubjectsAsync(Guid userId, CancellationToken ct)
        {
            var subjects = await _db.Subjects.Where(s => s.IsActive)
                .Select(s => new { s.Code, s.Name, Lessons = s.Lessons.Count(l => l.IsActive) })
                .ToListAsync(ct);

            var progress = await _db.LessonProgresses
                .Where(p => p.UserId == userId && p.CompletedAt != null)
                .Join(_db.Lessons, p => p.LessonId, l => l.Id, (p, l) => new { l.SubjectCode })
                .GroupBy(x => x.SubjectCode)
                .Select(g => new { SubjectCode = g.Key, Completed = g.Count() })
                .ToListAsync(ct);

            return subjects.Select(s =>
            {
                var done = progress.FirstOrDefault(x => x.SubjectCode == s.Code)?.Completed ?? 0;
                var pct = s.Lessons == 0 ? 0 : (int)Math.Round(100.0 * done / s.Lessons);
                return new SubjectTileDto(s.Code, s.Name, s.Lessons, done, pct);
            }).ToList();
        }

        public async Task<IReadOnlyList<LessonDto>> GetLessonsAsync(Guid userId, string subjectCode, CancellationToken ct)
        {
            var query =
                from l in _db.Lessons
                where l.IsActive && l.SubjectCode == subjectCode
                join p in _db.LessonProgresses.Where(x => x.UserId == userId) on l.Id equals p.LessonId into gp
                from p in gp.DefaultIfEmpty()
                orderby l.SortOrder, l.Title
                select new LessonDto(l.Id, l.Title, l.Summary, l.DurationMin, p != null && p.CompletedAt != null);

            return await query.ToListAsync(ct);
        }

        public async Task<LessonCompleteDto> CompleteLessonAsync(Guid userId, Guid lessonId, CancellationToken ct)
        {
            var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && l.IsActive, ct)
                         ?? throw new InvalidOperationException("Lesson not found.");

            var progress = await _db.LessonProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);

            var now = DateTime.UtcNow;

            if (progress == null)
            {
                progress = new Core.Entities.LessonProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    LessonId = lessonId,
                    StartedAt = now,
                    CompletedAt = now
                };
                _db.LessonProgresses.Add(progress);
            }
            else
            {
                progress.CompletedAt ??= now;
            }

            await _db.SaveChangesAsync(ct);


            var subjectCode = lesson.SubjectCode;
            var total = await _db.Lessons.CountAsync(l => l.SubjectCode == subjectCode && l.IsActive, ct);
            var done  = await _db.LessonProgresses
                .Where(p => p.UserId == userId && p.CompletedAt != null)
                .Join(_db.Lessons, p => p.LessonId, l => l.Id, (p, l) => l)
                .CountAsync(l => l.SubjectCode == subjectCode && l.IsActive, ct);

            if (total > 0 && done >= total)
            {
                var issued = await _db.Certificates.AnyAsync(c => c.UserId == userId && c.SubjectCode == subjectCode, ct);
                if (!issued)
                {
                    _db.Certificates.Add(new Core.Entities.Certificate
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        SubjectCode = subjectCode,
                        Title = $"Completed {subjectCode} Curriculum",
                        IssuedAt = now
                    });
                    await _db.SaveChangesAsync(ct);
                }
            }

            return new LessonCompleteDto(lessonId, progress.CompletedAt ?? now);
        }

        public async Task<IReadOnlyList<CertificateDto>> GetCertificatesAsync(Guid userId, CancellationToken ct)
        {
            return await _db.Certificates
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.IssuedAt)

                .Select(c => new CertificateDto(c.Id, c.Title, c.SubjectCode, c.IssuedAt, "Blue Sands LMS"))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(Guid userId, CancellationToken ct)
        {
            var subjects = await GetSubjectsAsync(userId, ct);
            var weakest = subjects.OrderBy(s => s.ProgressPercent).Take(2).ToList();

            return weakest
                .Select(w => new RecommendationDto(
                    SubjectCode: w.Code,
                    Topic: "Core Concepts",
                    Reason: $"Only {w.ProgressPercent}% complete in {w.Name}. Resume lessons to boost your mastery."))
                .ToList();
        }
    }
}
