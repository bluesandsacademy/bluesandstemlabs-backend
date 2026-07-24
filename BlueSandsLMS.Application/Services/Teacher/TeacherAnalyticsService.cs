using System;
using System.Collections.Generic;
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

    public sealed class TeacherAnalyticsService : ITeacherAnalyticsService
    {
        private readonly BlueSandsLMSDbContext _db;
        public TeacherAnalyticsService(BlueSandsLMSDbContext db) => _db = db;


        private static readonly Func<BlueSandsLMSDbContext, Guid, IEnumerable<Guid>> _qTeacherClassrooms =
            EF.CompileQuery((BlueSandsLMSDbContext db, Guid teacherId) =>
                db.Set<Enrollment>()
                  .AsNoTracking()
                  .Where(e => e.UserId == teacherId && e.RoleInClass == ClassRole.Teacher)
                  .Select(e => e.ClassroomId)
                  .Distinct());

        private static readonly Func<BlueSandsLMSDbContext, IEnumerable<Guid>, IEnumerable<Guid>> _qStudentIdsInClasses =
            EF.CompileQuery((BlueSandsLMSDbContext db, IEnumerable<Guid> classIds) =>
                db.Set<Enrollment>()
                  .AsNoTracking()
                  .Where(e => classIds.Contains(e.ClassroomId) && e.RoleInClass == ClassRole.Student)
                  .Select(e => e.UserId)
                  .Distinct());

        private static readonly Func<BlueSandsLMSDbContext, IEnumerable<Guid>, DateTime, DateTime, IEnumerable<ExperimentLaunch>> _qLaunchesInRange =
            EF.CompileQuery((BlueSandsLMSDbContext db, IEnumerable<Guid> classIds, DateTime from, DateTime to) =>
                db.Set<ExperimentLaunch>()
                  .AsNoTracking()
                  .Where(x => x.ClassroomId != null
                           && classIds.Contains(x.ClassroomId!.Value)
                           && x.StartedAt >= from && x.StartedAt <= to));

        private static readonly Func<BlueSandsLMSDbContext, IEnumerable<Guid>, DateTime, DateTime, IEnumerable<QuizAttempt>> _qQuizAttemptsInRange =
            EF.CompileQuery((BlueSandsLMSDbContext db, IEnumerable<Guid> classIds, DateTime from, DateTime to) =>
                db.Set<QuizAttempt>()
                  .AsNoTracking()
                  .Where(q => q.ClassroomId != null
                           && classIds.Contains(q.ClassroomId!.Value)
                           && ((q.CompletedAt ?? q.DateCreated) >= from)
                           && ((q.CompletedAt ?? q.DateCreated) <= to)));

        private static readonly Func<BlueSandsLMSDbContext, IEnumerable<Guid>, DateTime, DateTime, IEnumerable<Submission>> _qSubmissionsGradedInRange =
            EF.CompileQuery((BlueSandsLMSDbContext db, IEnumerable<Guid> classIds, DateTime from, DateTime to) =>
                db.Set<Submission>()
                  .AsNoTracking()
                  .Where(s => s.GradedAt.HasValue
                           && s.Assignment != null
                           && classIds.Contains(s.Assignment.ClassroomId)
                           && s.GradedAt >= from && s.GradedAt <= to));

        private static readonly Func<BlueSandsLMSDbContext, IEnumerable<Guid>, DateTime, DateTime, IEnumerable<Assignment>> _qAssignmentsInRange =
            EF.CompileQuery((BlueSandsLMSDbContext db, IEnumerable<Guid> classIds, DateTime from, DateTime to) =>
                db.Set<Assignment>()
                  .AsNoTracking()
                  .Where(a => classIds.Contains(a.ClassroomId)
                           && a.CreatedAt >= from && a.CreatedAt <= to));

        private static readonly Func<BlueSandsLMSDbContext, IEnumerable<Guid>, IEnumerable<User>> _qUsersByIds =
            EF.CompileQuery((BlueSandsLMSDbContext db, IEnumerable<Guid> userIds) =>
                db.Set<User>()
                  .AsNoTracking()
                  .Where(u => userIds.Contains(u.Id)));


        private async Task<Guid[]> GetTeacherClassrooms(Guid teacherId, Guid? classroomId, CancellationToken ct)
        {

            var fromEnrollments = _qTeacherClassrooms(_db, teacherId).ToList();

            var fromAssignment = await _db.ClassroomTeachers
                .AsNoTracking()
                .Where(ctr => ctr.TeacherUserId == teacherId)
                .Select(ctr => ctr.ClassroomId)
                .ToListAsync(ct);

            var all = fromEnrollments.Union(fromAssignment).Distinct().ToArray();
            if (classroomId is Guid cid) return all.Contains(cid) ? new[] { cid } : Array.Empty<Guid>();
            return all;
        }

        private async Task<Dictionary<Guid, string>> LoadUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
        {
            var ids = userIds.Distinct().ToArray();
            if (ids.Length == 0) return new Dictionary<Guid, string>();
            var pairs = _qUsersByIds(_db, ids)
                .Select(u => new { u.Id, u.FullName })
                .ToList();
            await Task.CompletedTask;
            return pairs.ToDictionary(x => x.Id, x => x.FullName ?? "");
        }


        public async Task<TeacherOverviewDto> OverviewAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct)
        {
            var classIds = await GetTeacherClassrooms(teacherId, classroomId, ct);
            if (classIds.Length == 0) return new TeacherOverviewDto();

            var now = DateTime.UtcNow;
            var today = now.Date;
            var start7d = to.AddDays(-7);
            var start30d = to.AddDays(-30);

            var activeToday = _qLaunchesInRange(_db, classIds, today, now)
                    .Select(x => x.UserId)
                .Concat(_qQuizAttemptsInRange(_db, classIds, today, now).Select(q => q.UserId))
                .Distinct()
                .Count();

            var active7d = _qLaunchesInRange(_db, classIds, start7d, to)
                    .Select(x => x.UserId)
                .Concat(_qQuizAttemptsInRange(_db, classIds, start7d, to).Select(q => q.UserId))
                .Distinct()
                .Count();

            var active30d = _qLaunchesInRange(_db, classIds, start30d, to)
                    .Select(x => x.UserId)
                .Concat(_qQuizAttemptsInRange(_db, classIds, start30d, to).Select(q => q.UserId))
                .Distinct()
                .Count();

            var TimeSpentSec7d = _qLaunchesInRange(_db, classIds, start7d, to)
                .Select(x => (int?)x.DurationSec)
                .Sum() ?? 0;


            var startedLessons = await _db.Set<LessonProgress>().AsNoTracking()
                .Where(lp => lp.StartedAt >= start30d && lp.StartedAt <= to)
                .CountAsync(ct);

            var completedLessons = await _db.Set<LessonProgress>().AsNoTracking()
                .Where(lp => lp.CompletedAt != null && lp.CompletedAt >= start30d && lp.CompletedAt <= to)
                .CountAsync(ct);

            var completionRate = startedLessons == 0 ? 0d : (double)completedLessons / startedLessons;

            var avgQuiz = _qQuizAttemptsInRange(_db, classIds, start30d, to)
                .Select(q => (double?)(q.Score0to1 * 100m))
                .DefaultIfEmpty(null)
                .Average() ?? 0;

            var avgSubm = _qSubmissionsGradedInRange(_db, classIds, start30d, to)
                .Select(s => (double?)(s.Score0to1 * 100m))
                .DefaultIfEmpty(null)
                .Average() ?? 0;

            var avgScorePercent = (avgQuiz == 0 && avgSubm == 0) ? 0
                : (avgQuiz == 0 ? avgSubm : (avgSubm == 0 ? avgQuiz : (avgQuiz + avgSubm) / 2.0));

            var mostLabs = _qLaunchesInRange(_db, classIds, start30d, to)
                .GroupBy(x => new { x.Subject, x.ExperimentName })
                .Select(g => new LabeledCount { Label = g.Key.Subject + ":" + g.Key.ExperimentName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToArray();

            return new TeacherOverviewDto
            {
                ActiveStudentsToday = activeToday,
                ActiveStudents7d = active7d,
                ActiveStudents30d = active30d,
                TimeSpentSec7d = TimeSpentSec7d,
                LessonCompletionRate = Math.Round(completionRate, 4),
                AvgScorePercent = Math.Round(avgScorePercent, 2),
                MostAttemptedLabs = mostLabs,
                ClassGenderSplit = new GenderSplit()
            };
        }


        public async Task<TeacherEngagementDto> EngagementAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct)
        {
            var classIds = await GetTeacherClassrooms(teacherId, classroomId, ct);
            if (classIds.Length == 0) return new TeacherEngagementDto();

            var launches = _qLaunchesInRange(_db, classIds, from, to)
                .Select(x => new { x.UserId, x.DurationSec, x.StartedAt })
                .ToList();

            var quizzes = _qQuizAttemptsInRange(_db, classIds, from, to)
                .Select(q => new { q.UserId, When = (q.CompletedAt ?? q.DateCreated) })
                .ToList();

            await Task.CompletedTask;

            var timeByStudent = launches.GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(v => v.DurationSec));

            var interactionsByStudent = launches.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Count());
            foreach (var q in quizzes.GroupBy(x => x.UserId))
                interactionsByStudent[q.Key] = (interactionsByStudent.TryGetValue(q.Key, out var c) ? c : 0) + q.Count();


            var lpStarted = await _db.Set<LessonProgress>().AsNoTracking()
                .Where(lp => lp.StartedAt >= from && lp.StartedAt <= to)
                .GroupBy(lp => lp.UserId)
                .Select(g => new { g.Key, Cnt = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Cnt, ct);

            var lpCompleted = await _db.Set<LessonProgress>().AsNoTracking()
                .Where(lp => lp.CompletedAt != null && lp.CompletedAt >= from && lp.CompletedAt <= to)
                .GroupBy(lp => lp.UserId)
                .Select(g => new { g.Key, Cnt = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Cnt, ct);

            var studentsInClasses = _qStudentIdsInClasses(_db, classIds).ToList();

            var byStudent = studentsInClasses.Select(sid =>
            {
                var started = lpStarted.TryGetValue(sid, out var st) ? st : 0;
                var completed = lpCompleted.TryGetValue(sid, out var cp) ? cp : 0;
                var rate = started == 0 ? 0 : Math.Min(1d, (double)completed / started);

                return new StudentEngagement
                {
                    StudentId = sid,
                    TimeSpentSec = timeByStudent.TryGetValue(sid, out var t) ? t : 0,
                    Interactions = interactionsByStudent.TryGetValue(sid, out var i) ? i : 0,
                    LessonCompletionRate = rate
                };
            })
            .OrderByDescending(x => x.TimeSpentSec)
            .ToArray();

            var peak = launches.GroupBy(x => x.StartedAt.Hour)
                .Select(g => new HourBucket { Hour = g.Key, Count = g.Count() })
                .OrderBy(x => x.Hour)
                .ToArray();

            var nameMap = await LoadUserNamesAsync(byStudent.Select(b => b.StudentId), ct);

            return new TeacherEngagementDto
            {
                ByStudent = byStudent,
                PeakUsage = peak,
                DisplayNames = nameMap
            };
        }


        public async Task<TeacherPerformanceDto> PerformanceAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct)
        {
            var classIds = await GetTeacherClassrooms(teacherId, classroomId, ct);
            if (classIds.Length == 0) return new TeacherPerformanceDto();

            var quizQ = _qQuizAttemptsInRange(_db, classIds, from, to)
                .Select(q => new
                {
                    StudentId = q.UserId,
                    When = (DateTime)(q.CompletedAt ?? q.DateCreated),
                    Percent = (double)(q.Score0to1 * 100m)
                });

            var submQ = _qSubmissionsGradedInRange(_db, classIds, from, to)
                .Select(s => new
                {
                    StudentId = s.StudentId,
                    When = s.GradedAt!.Value,
                    Percent = (double)(s.Score0to1 * 100m)
                });

            var scores = quizQ.Concat(submQ).ToList();
            await Task.CompletedTask;

            var avg = scores.Count == 0 ? 0 : scores.Average(x => x.Percent);

            var trend = scores
                .GroupBy(x => x.When.Date)
                .Select(g => new DayPoint { Date = DateOnly.FromDateTime(g.Key), Avg = g.Average(x => x.Percent) })
                .OrderBy(x => x.Date)
                .ToArray();

            var top = scores
                .GroupBy(x => x.StudentId)
                .Select(g => new StudentScore { StudentId = g.Key, AvgPercent = g.Average(x => x.Percent), Attempts = g.Count() })
                .Where(s => s.Attempts >= 3)
                .OrderByDescending(s => s.AvgPercent)
                .Take(10)
                .ToArray();

            var start7d = to.AddDays(-7);

            var recent = scores.Where(x => x.When >= start7d).ToList();
            var recentAgg = recent.GroupBy(x => x.StudentId)
                .ToDictionary(g => g.Key, g => new { Attempts = g.Count(), Avg = g.Average(x => x.Percent) });

            var allStudents = _qStudentIdsInClasses(_db, classIds).ToList();

            var active7d = _qLaunchesInRange(_db, classIds, start7d, to)
                    .Select(x => x.UserId)
                .Concat(_qQuizAttemptsInRange(_db, classIds, start7d, to).Select(q => q.UserId))
                .Distinct()
                .ToList();

            var atRisk = new List<AtRiskStudent>();
            foreach (var sid in allStudents)
            {
                var reasons = new List<string>();
                if (!active7d.Contains(sid)) reasons.Add("Inactivity7d");
                if (recentAgg.TryGetValue(sid, out var r) && r.Attempts >= 2 && r.Avg < 50) reasons.Add("LowScore");
                if (reasons.Count > 0) atRisk.Add(new AtRiskStudent { StudentId = sid, Reason = reasons.ToArray() });
            }

            var nameMap = await LoadUserNamesAsync(allStudents, ct);

            return new TeacherPerformanceDto
            {
                AvgScorePercent = Math.Round(avg, 2),
                Trend = trend,
                TopPerformers = top,
                AtRisk = atRisk.ToArray(),
                DisplayNames = nameMap
            };
        }


        public async Task<TeacherAssignmentsDto> AssignmentsAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct)
        {
            var classIds = await GetTeacherClassrooms(teacherId, classroomId, ct);
            if (classIds.Length == 0) return new TeacherAssignmentsDto();

            var assignments = _qAssignmentsInRange(_db, classIds, from, to)
                .Select(a => new { a.Id, a.Title, a.DueAt, a.ClassroomId })
                .ToList();

            var subs = await _db.Set<Submission>().AsNoTracking()
                .Where(s => s.SubmittedAt != null && s.Assignment != null && classIds.Contains(s.Assignment.ClassroomId)
                         && s.SubmittedAt >= from && s.SubmittedAt <= to)
                .Select(s => new
                {
                    s.AssignmentId,
                    s.SubmittedAt,
                    s.GradedAt,
                    s.Score0to1,
                    DueAt = s.Assignment!.DueAt
                })
                .ToListAsync(ct);

            var created = assignments.Count;
            var submitted = subs.Count;

            var late = subs.Count(s => s.DueAt != null && s.SubmittedAt!.Value > s.DueAt!.Value);
            var latePct = submitted == 0 ? 0d : (double)late / submitted;

            var turnaroundMsAvg = subs.Where(s => s.GradedAt != null)
                .Select(s => (s.GradedAt.GetValueOrDefault() - s.SubmittedAt.GetValueOrDefault()).TotalMilliseconds)
                .DefaultIfEmpty(0)
                .Average();

            var per = subs.GroupBy(s => s.AssignmentId)
                .Select(g => new
                {
                    AssignmentId = g.Key,
                    Submitted = g.Count(),
                    Late = g.Count(x => x.DueAt != null && x.SubmittedAt!.Value > x.DueAt!.Value),
                    AvgScore = g.Average(x => (double?)(x.Score0to1 * 100m)) ?? 0
                }).ToList();

            var map = assignments.ToDictionary(x => x.Id, x => x);
            var lines = per.Select(p => new AssignmentLine
            {
                Id = p.AssignmentId,
                Title = map.TryGetValue(p.AssignmentId, out var a) ? a.Title : "(deleted)",
                DueAt = map.TryGetValue(p.AssignmentId, out var b) ? b.DueAt : null,
                Submitted = p.Submitted,
                Late = p.Late,
                AvgScorePercent = Math.Round(p.AvgScore, 2)
            })
            .OrderByDescending(x => x.Submitted)
            .ToArray();

            return new TeacherAssignmentsDto
            {
                Created = created,
                Submitted = submitted,
                LatePct = Math.Round(latePct, 4),
                TurnaroundMsAvg = Math.Round(turnaroundMsAvg, 2),
                ByAssignment = lines
            };
        }


        public async Task<TeacherAttendanceDto> AttendanceAsync(Guid teacherId, Guid? classroomId, string? subject, DateTime from, DateTime to, CancellationToken ct)
        {
            var classIds = await GetTeacherClassrooms(teacherId, classroomId, ct);
            if (classIds.Length == 0) return new TeacherAttendanceDto();

            var now = DateTime.UtcNow;
            var today = now.Date;
            var fiveMinAgo = now.AddMinutes(-5);

            var onlineNow = _qLaunchesInRange(_db, classIds, fiveMinAgo, now)
                    .Select(x => x.UserId)
                .Concat(_qQuizAttemptsInRange(_db, classIds, fiveMinAgo, now).Select(q => q.UserId))
                .Distinct()
                .Count();

            var activeToday = _qLaunchesInRange(_db, classIds, today, now)
                    .Select(x => x.UserId)
                .Concat(_qQuizAttemptsInRange(_db, classIds, today, now).Select(q => q.UserId))
                .Distinct()
                .Count();

            var launches = _qLaunchesInRange(_db, classIds, from, to)
                .Select(x => new { x.UserId, x.StartedAt })
                .ToList();

            await Task.CompletedTask;

            var daily = launches
                .GroupBy(x => x.StartedAt.Date)
                .Select(g => new DayActive
                {
                    Date = DateOnly.FromDateTime(g.Key),
                    Active = g.Select(v => v.UserId).Distinct().Count()
                })
                .OrderBy(x => x.Date)
                .ToArray();

            return new TeacherAttendanceDto { OnlineNow = onlineNow, ActiveToday = activeToday, ByDay = daily };
        }
    }
}
