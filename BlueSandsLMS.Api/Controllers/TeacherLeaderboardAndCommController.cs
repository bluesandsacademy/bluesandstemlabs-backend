using System.Security.Claims;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{

    [ApiController]
    public sealed class TeacherLeaderboardAndCommController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        public TeacherLeaderboardAndCommController(BlueSandsLMSDbContext db) => _db = db;

        private Guid Me()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private Guid? MySchoolId()
        {
            var s = User.FindFirstValue("SchoolId");
            return Guid.TryParse(s, out var id) ? id : (Guid?)null;
        }

        [HttpGet("api/teacher/communication-metrics")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CommunicationMetrics([FromQuery] int days = 30, CancellationToken ct = default)
        {
            days = Math.Clamp(days <= 0 ? 30 : days, 7, 120);
            var since = DateTime.UtcNow.AddDays(-days);
            var me = Me();

            var messagesSent = await _db.Set<MessageLog>().AsNoTracking()
                .CountAsync(m => m.FromUserId == me && m.SentAt >= since, ct);

            var directMessagesSent = await _db.Set<MessageLog>().AsNoTracking()
                .CountAsync(m => m.FromUserId == me && m.ToUserId != null && m.SentAt >= since, ct);

            var directMessagesRead = await _db.Set<MessageLog>().AsNoTracking()
                .CountAsync(m => m.FromUserId == me && m.ToUserId != null && m.SentAt >= since && m.ReadAt != null, ct);

            var announcementsPosted = await _db.Announcements.AsNoTracking()
                .CountAsync(a => a.CreatedByUserId == me && a.CreatedAt >= since, ct);

            var responseRate = directMessagesSent == 0 ? 0.0
                : Math.Round(100.0 * directMessagesRead / directMessagesSent, 1);

            return Ok(new
            {
                messagesSent,
                directMessagesSent,
                directMessagesRead,
                announcementsPosted,
                responseRatePercent = responseRate
            });
        }

        [HttpGet("api/leaderboard/teachers")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> TeacherLeaderboard([FromQuery] int take = 10, CancellationToken ct = default)
        {
            take = Math.Clamp(take <= 0 ? 10 : take, 1, 50);
            var sid = MySchoolId();
            if (sid is null) return BadRequest(new { message = "SchoolId missing in token." });

            var classIds = await _db.Classrooms.AsNoTracking()
                .Where(c => c.SchoolId == sid.Value)
                .Select(c => c.Id)
                .ToListAsync(ct);

            var classAverages = await _db.QuizAttempts.AsNoTracking()
                .Where(q => q.ClassroomId != null && classIds.Contains(q.ClassroomId.Value))
                .GroupBy(q => q.ClassroomId!.Value)
                .Select(g => new { ClassId = g.Key, Avg = g.Average(x => (double)x.Score0to1) })
                .ToListAsync(ct);

            var ctLinks = await _db.ClassroomTeachers.AsNoTracking()
                .Where(t => classIds.Contains(t.ClassroomId))
                .Select(t => new { t.ClassroomId, TeacherId = t.TeacherUserId })
                .ToListAsync(ct);

            var enrollLinks = await _db.Enrollments.AsNoTracking()
                .Where(e => classIds.Contains(e.ClassroomId) && e.RoleInClass == ClassRole.Teacher)
                .Select(e => new { e.ClassroomId, TeacherId = e.UserId })
                .ToListAsync(ct);

            var links = ctLinks.Concat(enrollLinks).Distinct().ToList();

            var avgByClass = classAverages.ToDictionary(x => x.ClassId, x => x.Avg);

            var perTeacher = links
                .GroupBy(l => l.TeacherId)
                .Select(g =>
                {
                    var scores = g.Where(x => avgByClass.ContainsKey(x.ClassroomId))
                                  .Select(x => avgByClass[x.ClassroomId])
                                  .ToList();
                    return new
                    {
                        TeacherId = g.Key,
                        AvgScorePercent = scores.Count == 0 ? 0.0 : Math.Round(scores.Average() * 100.0, 2),
                        ClassesCounted = scores.Count
                    };
                })
                .OrderByDescending(x => x.AvgScorePercent)
                .Take(take)
                .ToList();

            var teacherIds = perTeacher.Select(p => p.TeacherId).ToList();
            var names = await _db.Users.AsNoTracking()
                .Where(u => teacherIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToListAsync(ct);

            var entries = perTeacher.Select((p, i) => new
            {
                rank = i + 1,
                teacherId = p.TeacherId,
                teacherName = names.FirstOrDefault(n => n.Id == p.TeacherId)?.FullName ?? "(Teacher)",
                avgScorePercent = p.AvgScorePercent,
                classesCounted = p.ClassesCounted
            }).ToList();

            return Ok(entries);
        }
    }
}
