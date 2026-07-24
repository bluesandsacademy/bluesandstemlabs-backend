
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.Interfaces.Teacher;
using BlueSandsLMS.Common.Teacher;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Application.Services.Teacher
{
    public sealed class TeacherCommAnalyticsService : ITeacherCommAnalyticsService
    {
        private readonly BlueSandsLMSDbContext _db;
        public TeacherCommAnalyticsService(BlueSandsLMSDbContext db) => _db = db;

        private async Task<Guid[]> ClassIds(Guid teacherId, Guid? classroomId, CancellationToken ct)
        {
            var q = _db.Set<Enrollment>().AsNoTracking()
                .Where(e => e.UserId == teacherId && e.RoleInClass == ClassRole.Teacher)
                .Select(e => e.ClassroomId)
                .Distinct();

            var all = await q.ToArrayAsync(ct);
            if (classroomId is Guid cid) return all.Contains(cid) ? new[] { cid } : Array.Empty<Guid>();
            return all;
        }

        public async Task<TeacherCommOverviewDto> CommOverviewAsync(Guid teacherId, Guid? classroomId, DateTime from, DateTime to, CancellationToken ct)
        {
            var classIds = await ClassIds(teacherId, classroomId, ct);
            if (classIds.Length == 0) return new TeacherCommOverviewDto();

            var ml = _db.Set<MessageLog>().AsNoTracking()
                .Where(m => m.SentAt >= from && m.SentAt <= to &&
                            (m.ClassroomId == null || classIds.Contains(m.ClassroomId.Value)));

            var sent = await ml.CountAsync(m => m.FromUserId == teacherId, ct);
            var received = await ml.CountAsync(m => m.ToUserId == teacherId, ct);
            var unread = await ml.CountAsync(m => m.ToUserId == teacherId && m.ReadAt == null, ct);


            var replies = await ml.Where(m => m.ThreadId != null).ToListAsync(ct);
            var avgMinutes = 0d;
            if (replies.Count > 0)
            {
                var groups = replies.GroupBy(x => x.ThreadId);
                var deltas = groups.SelectMany(g =>
                {
                    var ordered = g.OrderBy(x => x.SentAt).ToList();
                    var res = new System.Collections.Generic.List<double>();
                    for (int i = 0; i < ordered.Count - 1; i++)
                    {
                        var a = ordered[i];
                        var b = ordered[i + 1];

                        if (a.FromUserId == teacherId && b.FromUserId != teacherId)
                            res.Add((b.SentAt - a.SentAt).TotalMinutes);
                        else if (a.FromUserId != teacherId && b.FromUserId == teacherId)
                            res.Add((b.SentAt - a.SentAt).TotalMinutes);
                    }
                    return res;
                }).ToList();
                avgMinutes = deltas.Count == 0 ? 0 : deltas.Average();
            }

            var topThreads = await ml.Where(m => m.ThreadId != null)
                .GroupBy(m => m.ThreadId!)
                .Select(g => new LabeledCount { Label = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToArrayAsync(ct);

            return new TeacherCommOverviewDto
            {
                MessagesSent = sent,
                MessagesReceived = received,
                UnreadReceived = unread,
                AvgResponseTimeMinutes = Math.Round(avgMinutes, 2),
                TopThreads = topThreads
            };
        }

        public async Task<TeacherForumOverviewDto> ForumOverviewAsync(Guid teacherId, Guid? classroomId, DateTime from, DateTime to, CancellationToken ct)
        {
            var classIds = await ClassIds(teacherId, classroomId, ct);
            if (classIds.Length == 0) return new TeacherForumOverviewDto();

            var topics = _db.Set<ForumTopic>().AsNoTracking()
                .Where(t => classIds.Contains(t.ClassroomId) && t.CreatedAt >= from && t.CreatedAt <= to);

            var posts = _db.Set<ForumPost>().AsNoTracking()
                .Where(p => p.Topic != null && classIds.Contains(p.Topic!.ClassroomId) &&
                            p.CreatedAt >= from && p.CreatedAt <= to);

            var topicsCreated = await topics.CountAsync(ct);
            var postsMade = await posts.CountAsync(ct);

            var uniqueParticipants = await posts.Select(p => p.UserId).Distinct().CountAsync(ct);

            var trend = await posts.GroupBy(p => p.CreatedAt.Date)
                .Select(g => new DayPoint { Date = DateOnly.FromDateTime(g.Key), Avg = g.Count() })
                .OrderBy(x => x.Date)
                .ToArrayAsync(ct);

            var busiest = await posts.GroupBy(p => new { p.TopicId, p.Topic!.Title })
                .Select(g => new LabeledCount { Label = g.Key.Title, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToArrayAsync(ct);

            return new TeacherForumOverviewDto
            {
                TopicsCreated = topicsCreated,
                PostsMade = postsMade,
                UniqueParticipants = uniqueParticipants,
                ActivityTrend = trend,
                BusiestTopics = busiest
            };
        }
    }
}
