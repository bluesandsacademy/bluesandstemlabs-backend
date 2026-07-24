using System.Security.Claims;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{

    [ApiController]
    [Route("api/teacher-analytics/v1")]
    [Authorize(Roles = "Teacher")]
    public sealed class TeacherAnalyticsExtV1Controller : ControllerBase
    {
        private static readonly string[] MonthNames =
            { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        private static readonly string[] DayNames = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        private readonly BlueSandsLMSDbContext _db;
        public TeacherAnalyticsExtV1Controller(BlueSandsLMSDbContext db) => _db = db;

        private Guid Me()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private async Task<List<Guid>> GetTeacherClassroomIdsAsync(Guid teacherId, CancellationToken ct)
        {
            var fromEnrollments = await _db.Enrollments.AsNoTracking()
                .Where(e => e.UserId == teacherId && e.RoleInClass == ClassRole.Teacher)
                .Select(e => e.ClassroomId)
                .ToListAsync(ct);

            var fromClassroomTeachers = await _db.ClassroomTeachers.AsNoTracking()
                .Where(t => t.TeacherUserId == teacherId)
                .Select(t => t.ClassroomId)
                .ToListAsync(ct);

            return fromEnrollments.Concat(fromClassroomTeachers).Distinct().ToList();
        }

        private async Task<List<Guid>> GetStudentIdsAsync(List<Guid> classroomIds, CancellationToken ct)
        {
            if (classroomIds.Count == 0) return new List<Guid>();
            return await _db.Enrollments.AsNoTracking()
                .Where(e => classroomIds.Contains(e.ClassroomId) && e.RoleInClass == ClassRole.Student)
                .Select(e => e.UserId)
                .Distinct()
                .ToListAsync(ct);
        }

        private static double[] NewMonthlyBucket() => new double[12];

        [HttpGet("overview")]
        public async Task<IActionResult> Overview(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);

            var totalIlsCreated = await _db.InteractiveLearningSpaces.AsNoTracking()
                .CountAsync(i => i.CreatedBy == teacherId, ct);

            var totalAssignments = classroomIds.Count == 0 ? 0
                : await _db.Assignments.AsNoTracking().CountAsync(a => classroomIds.Contains(a.ClassroomId), ct);

            var assignmentIds = classroomIds.Count == 0 ? new List<Guid>()
                : await _db.Assignments.AsNoTracking()
                    .Where(a => classroomIds.Contains(a.ClassroomId))
                    .Select(a => a.Id)
                    .ToListAsync(ct);

            var pendingToGrade = assignmentIds.Count == 0 ? 0
                : await _db.Submissions.AsNoTracking()
                    .CountAsync(s => assignmentIds.Contains(s.AssignmentId) && s.Status == SubmissionStatus.Submitted, ct);

            var sessions = studentIds.Count == 0 ? new List<StudentIlsSession>()
                : await _db.StudentIlsSessions.AsNoTracking()
                    .Where(s => studentIds.Contains(s.StudentId))
                    .Include(s => s.Assessment)
                    .ToListAsync(ct);

            var experimentsCompleted = sessions.Count(s => s.CompletedAt != null);

            var scores = sessions.Where(s => s.Assessment != null).Select(s => (double)s.Assessment!.Score * 100.0).ToList();
            var avgClassScore = scores.Count == 0 ? 0.0 : Math.Round(scores.Average(), 2);

            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var activeStudentsThisWeek = sessions
                .Where(s => s.UpdatedAt >= weekAgo)
                .Select(s => s.StudentId)
                .Distinct()
                .Count();

            var names = studentIds.Count == 0 ? new Dictionary<Guid, string>()
                : (await _db.Users.AsNoTracking().Where(u => studentIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName }).ToListAsync(ct))
                    .ToDictionary(x => x.Id, x => x.FullName);

            var classroomNameByStudent = new Dictionary<Guid, string>();
            if (classroomIds.Count > 0)
            {
                var enrollRows = await _db.Enrollments.AsNoTracking()
                    .Where(e => classroomIds.Contains(e.ClassroomId) && e.RoleInClass == ClassRole.Student)
                    .Select(e => new { e.UserId, e.ClassroomId })
                    .ToListAsync(ct);
                var classNames = await _db.Classrooms.AsNoTracking()
                    .Where(c => classroomIds.Contains(c.Id))
                    .Select(c => new { c.Id, c.Name })
                    .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
                foreach (var row in enrollRows)
                    if (!classroomNameByStudent.ContainsKey(row.UserId) && classNames.TryGetValue(row.ClassroomId, out var cn))
                        classroomNameByStudent[row.UserId] = cn;
            }

            var perStudent = sessions
                .GroupBy(s => s.StudentId)
                .Select(g => new
                {
                    userId = g.Key,
                    studentName = names.TryGetValue(g.Key, out var n) ? n : "Unknown",
                    avgScore = g.Where(s => s.Assessment != null).Select(s => (double)s.Assessment!.Score * 100.0).DefaultIfEmpty(0.0).Average(),
                    experimentsCompleted = g.Count(s => s.CompletedAt != null),
                    classroomName = classroomNameByStudent.TryGetValue(g.Key, out var cn2) ? cn2 : ""
                })
                .ToList();

            var topPerforming = perStudent
                .OrderByDescending(s => s.avgScore)
                .Take(5)
                .Select(s => new { s.userId, s.studentName, avgScore = Math.Round(s.avgScore, 2), s.experimentsCompleted, s.classroomName })
                .ToList();

            var atRisk = perStudent
                .Where(s => s.experimentsCompleted == 0 || s.avgScore < 50.0)
                .OrderBy(s => s.avgScore)
                .Take(5)
                .Select(s => new
                {
                    s.userId,
                    s.studentName,
                    avgScore = Math.Round(s.avgScore, 2),
                    s.experimentsCompleted,
                    s.classroomName,
                    reason = s.experimentsCompleted == 0 ? "No activity" : "Low score"
                })
                .ToList();

            return Ok(new
            {
                totalStudents = studentIds.Count,
                totalClasses = classroomIds.Count,
                totalIlsCreated,
                totalAssignments,
                avgClassScore,
                experimentsCompleted,
                activeStudentsThisWeek,
                pendingToGrade,
                topPerforming,
                atRisk
            });
        }

        [HttpGet("performance-trends")]
        public async Task<IActionResult> PerformanceTrends(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);

            var rows = studentIds.Count == 0 ? new List<(int Month, double Score)>()
                : await _db.SessionAssessments.AsNoTracking()
                    .Join(_db.StudentIlsSessions.AsNoTracking(), a => a.SessionId, s => s.Id, (a, s) => new { a.Score, a.SubmittedAt, s.StudentId })
                    .Where(x => studentIds.Contains(x.StudentId))
                    .Select(x => new { x.Score, x.SubmittedAt })
                    .ToListAsync(ct)
                    .ContinueWith(t => t.Result.Select(x => (x.SubmittedAt.Month, (double)x.Score * 100.0)).ToList(), ct);

            var buckets = NewMonthlyBucket();
            var counts = new int[12];
            foreach (var (month, score) in rows)
            {
                buckets[month - 1] += score;
                counts[month - 1]++;
            }

            var trends = Enumerable.Range(0, 12)
                .Select(i => new { month = MonthNames[i], average = counts[i] == 0 ? 0.0 : Math.Round(buckets[i] / counts[i], 2) })
                .ToList();

            return Ok(new { trends });
        }

        [HttpGet("time-spent")]
        public async Task<IActionResult> TimeSpent(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);

            var now = DateTime.UtcNow;
            var mondayOffset = ((int)now.DayOfWeek + 6) % 7;
            var weekStart = now.Date.AddDays(-mondayOffset);
            var weekEnd = weekStart.AddDays(7);

            var launches = studentIds.Count == 0 ? new List<(int Day, int DurationSec)>()
                : await _db.ExperimentLaunches.AsNoTracking()
                    .Where(e => studentIds.Contains(e.UserId) && e.DateCreated >= weekStart && e.DateCreated < weekEnd)
                    .Select(e => new { e.DateCreated, e.DurationSec })
                    .ToListAsync(ct)
                    .ContinueWith(t => t.Result.Select(x => (((int)x.DateCreated.DayOfWeek + 6) % 7, x.DurationSec)).ToList(), ct);

            var minutesPerDay = new double[7];
            foreach (var (day, durationSec) in launches)
                minutesPerDay[day] += durationSec / 60.0;

            var weeklyData = Enumerable.Range(0, 7)
                .Select(i => new { day = DayNames[i], time = Math.Round(minutesPerDay[i], 1) })
                .ToList();

            return Ok(new { weeklyData });
        }

        [HttpGet("class-improvement")]
        public async Task<IActionResult> ClassImprovement(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);

            var sessions = studentIds.Count == 0 ? new List<StudentIlsSession>()
                : await _db.StudentIlsSessions.AsNoTracking()
                    .Where(s => studentIds.Contains(s.StudentId))
                    .Include(s => s.Assessment)
                    .ToListAsync(ct);

            var scoreSum = new double[12];
            var scoreCount = new int[12];
            var activeStudentsByMonth = new HashSet<Guid>[12];
            var completedByMonth = new int[12];
            for (var i = 0; i < 12; i++) activeStudentsByMonth[i] = new HashSet<Guid>();

            foreach (var s in sessions)
            {
                if (s.Assessment != null)
                {
                    var m = s.Assessment.SubmittedAt.Month - 1;
                    scoreSum[m] += (double)s.Assessment.Score * 100.0;
                    scoreCount[m]++;
                }
                if (s.CompletedAt != null)
                {
                    var m = s.CompletedAt.Value.Month - 1;
                    completedByMonth[m]++;
                    activeStudentsByMonth[m].Add(s.StudentId);
                }
            }

            var trends = Enumerable.Range(0, 12).Select(i => new
            {
                month = MonthNames[i],
                average = scoreCount[i] == 0 ? 0.0 : Math.Round(scoreSum[i] / scoreCount[i], 2),
                attendance = activeStudentsByMonth[i].Count,
                lab_completion = completedByMonth[i]
            }).ToList();

            return Ok(new { trends });
        }

        [HttpGet("average-scores")]
        public async Task<IActionResult> AverageScores(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);

            var subjectData = await BuildSubjectMetricsAsync(studentIds, ct);
            return Ok(new { subjects = subjectData });
        }

        private async Task<List<object>> BuildSubjectMetricsAsync(List<Guid> studentIds, CancellationToken ct)
        {
            if (studentIds.Count == 0) return new List<object> { new { subject = "General", average = 0.0, attendance = 0, lab_completion = 0 } };

            var ilsSubjects = await _db.IlsTags.AsNoTracking()
                .Join(_db.CurriculumTags.AsNoTracking(), t => t.TagId, c => c.Id, (t, c) => new { t.IlsId, c.Subject })
                .ToListAsync(ct);
            var subjectByIls = ilsSubjects.GroupBy(x => x.IlsId).ToDictionary(g => g.Key, g => g.First().Subject);

            var sessions = await _db.StudentIlsSessions.AsNoTracking()
                .Where(s => studentIds.Contains(s.StudentId))
                .Include(s => s.Assessment)
                .ToListAsync(ct);

            var bySubject = sessions
                .Select(s => new { Subject = subjectByIls.TryGetValue(s.IlsId, out var subj) ? subj : "General", Session = s })
                .GroupBy(x => x.Subject)
                .Select(g => new
                {
                    subject = string.IsNullOrWhiteSpace(g.Key) ? "General" : g.Key,
                    average = g.Where(x => x.Session.Assessment != null)
                        .Select(x => (double)x.Session.Assessment!.Score * 100.0)
                        .DefaultIfEmpty(0.0).Average(),
                    attendance = g.Select(x => x.Session.StudentId).Distinct().Count(),
                    lab_completion = g.Count(x => x.Session.CompletedAt != null)
                })
                .Select(x => (object)new { x.subject, average = Math.Round(x.average, 2), x.attendance, x.lab_completion })
                .ToList();

            return bySubject.Count == 0
                ? new List<object> { new { subject = "General", average = 0.0, attendance = 0, lab_completion = 0 } }
                : bySubject;
        }

        [HttpGet("assignments")]
        public async Task<IActionResult> Assignments(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);

            var assignments = classroomIds.Count == 0 ? new List<Assignment>()
                : await _db.Assignments.AsNoTracking().Where(a => classroomIds.Contains(a.ClassroomId)).ToListAsync(ct);

            var assignmentIds = assignments.Select(a => a.Id).ToList();
            var submissions = assignmentIds.Count == 0 ? new List<Submission>()
                : await _db.Submissions.AsNoTracking().Where(s => assignmentIds.Contains(s.AssignmentId)).ToListAsync(ct);

            var createdByMonth = new int[12];
            foreach (var a in assignments) createdByMonth[a.CreatedAt.Month - 1]++;

            var submittedByMonth = new int[12];
            foreach (var s in submissions.Where(s => s.SubmittedAt != null)) submittedByMonth[s.SubmittedAt!.Value.Month - 1]++;

            var data = Enumerable.Range(0, 12)
                .Select(i => new { month = MonthNames[i], created = createdByMonth[i], submitted = submittedByMonth[i] })
                .ToList();

            var studentCountByClassroom = classroomIds.Count == 0 ? new Dictionary<Guid, int>()
                : (await _db.Enrollments.AsNoTracking()
                    .Where(e => classroomIds.Contains(e.ClassroomId) && e.RoleInClass == ClassRole.Student)
                    .GroupBy(e => e.ClassroomId)
                    .Select(g => new { ClassroomId = g.Key, Count = g.Select(e => e.UserId).Distinct().Count() })
                    .ToListAsync(ct))
                    .ToDictionary(x => x.ClassroomId, x => x.Count);

            var now = DateTime.UtcNow;
            var submissionsTable = assignments.Select(a =>
            {
                var subs = submissions.Where(s => s.AssignmentId == a.Id).ToList();
                var graded = subs.Where(s => s.Status == SubmissionStatus.Graded).ToList();
                return (object)new
                {
                    assignmentId = a.Id,
                    title = a.Title,
                    type = a.Type.ToString().ToLowerInvariant(),
                    dueAt = a.DueAt,
                    totalStudents = studentCountByClassroom.TryGetValue(a.ClassroomId, out var tc) ? tc : 0,
                    submitted = subs.Count(s => s.SubmittedAt != null),
                    graded = graded.Count,
                    avgScore = graded.Count == 0 ? 0.0 : Math.Round((double)graded.Average(s => s.Score0to1 ?? 0m) * 100.0, 2),
                    status = a.DueAt == null || a.DueAt >= now ? "active" : "closed"
                };
            }).ToList();

            return Ok(new { data, submissions = submissionsTable });
        }

        [HttpGet("feedback")]
        public async Task<IActionResult> Feedback(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);

            var assignmentIds = classroomIds.Count == 0 ? new List<Guid>()
                : await _db.Assignments.AsNoTracking().Where(a => classroomIds.Contains(a.ClassroomId)).Select(a => a.Id).ToListAsync(ct);

            var graded = assignmentIds.Count == 0 ? new List<DateTime>()
                : await _db.Submissions.AsNoTracking()
                    .Where(s => assignmentIds.Contains(s.AssignmentId) && s.GradedAt != null)
                    .Select(s => s.GradedAt!.Value)
                    .ToListAsync(ct);

            var byMonth = new int[12];
            foreach (var d in graded) byMonth[d.Month - 1]++;

            var data = Enumerable.Range(0, 12).Select(i => new { month = MonthNames[i], feedback = byMonth[i] }).ToList();
            return Ok(new { data });
        }

        [HttpGet("communications")]
        public async Task<IActionResult> Communications(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);
            var studentIdSet = studentIds.ToHashSet();

            var messages = classroomIds.Count == 0 ? new List<ClassMessage>()
                : await _db.ClassMessages.AsNoTracking().Where(m => classroomIds.Contains(m.ClassroomId)).ToListAsync(ct);

            var now = DateTime.UtcNow;
            var sixMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

            var teacherToStudent = new int[6];
            var studentToTeacher = new int[6];
            foreach (var m in messages.Where(m => m.SentAt >= sixMonthsAgo))
            {
                var idx = ((m.SentAt.Year - sixMonthsAgo.Year) * 12 + m.SentAt.Month - sixMonthsAgo.Month);
                if (idx < 0 || idx > 5) continue;
                if (m.FromUserId == teacherId) teacherToStudent[idx]++;
                else if (studentIdSet.Contains(m.FromUserId)) studentToTeacher[idx]++;
            }

            var messagesTrend = Enumerable.Range(0, 6).Select(i =>
            {
                var monthDate = sixMonthsAgo.AddMonths(i);
                var row = new Dictionary<string, object>
                {
                    ["month"] = MonthNames[monthDate.Month - 1],
                    ["teacher-student"] = teacherToStudent[i],
                    ["student-teacher"] = studentToTeacher[i]
                };
                return row;
            }).Cast<object>().ToList();

            var participationByMonth = new HashSet<Guid>[12];
            for (var i = 0; i < 12; i++) participationByMonth[i] = new HashSet<Guid>();
            foreach (var m in messages) participationByMonth[m.SentAt.Month - 1].Add(m.FromUserId);
            var participationTrend = Enumerable.Range(0, 12)
                .Select(i => new { month = MonthNames[i], activity = participationByMonth[i].Count })
                .ToList();

            var announcements = messages.Count(m => m.FromUserId == teacherId);
            var assignmentsCount = classroomIds.Count == 0 ? 0
                : await _db.Assignments.AsNoTracking().CountAsync(a => classroomIds.Contains(a.ClassroomId), ct);

            var ilsIds = await _db.InteractiveLearningSpaces.AsNoTracking()
                .Where(i => i.CreatedBy == teacherId).Select(i => i.Id).ToListAsync(ct);
            var questions = ilsIds.Count == 0 ? 0
                : await _db.IlsDiscussionMessages.AsNoTracking().CountAsync(d => ilsIds.Contains(d.IlsId) && studentIdSet.Contains(d.AuthorId), ct);

            var assignmentIds = classroomIds.Count == 0 ? new List<Guid>()
                : await _db.Assignments.AsNoTracking().Where(a => classroomIds.Contains(a.ClassroomId)).Select(a => a.Id).ToListAsync(ct);
            var feedbacks = assignmentIds.Count == 0 ? 0
                : await _db.Submissions.AsNoTracking()
                    .CountAsync(s => assignmentIds.Contains(s.AssignmentId) && s.Feedback != null && s.Feedback != "", ct);

            var messageTypes = new object[]
            {
                new { name = "Announcements", value = announcements },
                new { name = "Assignments", value = assignmentsCount },
                new { name = "Questions", value = questions },
                new { name = "Feedbacks", value = feedbacks }
            };

            return Ok(new { messagesTrend, participationTrend, messageTypes });
        }

        [HttpGet("attendance")]
        public async Task<IActionResult> Attendance(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);
            var totalStudents = studentIds.Count;

            var completions = studentIds.Count == 0 ? new List<(int Month, Guid StudentId)>()
                : await _db.StudentIlsSessions.AsNoTracking()
                    .Where(s => studentIds.Contains(s.StudentId) && s.CompletedAt != null)
                    .Select(s => new { s.CompletedAt, s.StudentId })
                    .ToListAsync(ct)
                    .ContinueWith(t => t.Result.Select(x => (x.CompletedAt!.Value.Month, x.StudentId)).ToList(), ct);

            var presentByMonth = new HashSet<Guid>[12];
            for (var i = 0; i < 12; i++) presentByMonth[i] = new HashSet<Guid>();
            foreach (var (month, studentId) in completions) presentByMonth[month - 1].Add(studentId);

            var trends = Enumerable.Range(0, 12).Select(i =>
            {
                var present = presentByMonth[i].Count;
                return new
                {
                    month = MonthNames[i],
                    late = 0,
                    present,
                    absent = Math.Max(0, totalStudents - present)
                };
            }).ToList();

            return Ok(new { trends });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> Reports(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);

            var subjectData = await BuildSubjectMetricsAsync(studentIds, ct);

            var totalStudents = studentIds.Count;
            var completions = studentIds.Count == 0 ? new List<(int Month, Guid StudentId)>()
                : await _db.StudentIlsSessions.AsNoTracking()
                    .Where(s => studentIds.Contains(s.StudentId) && s.CompletedAt != null)
                    .Select(s => new { s.CompletedAt, s.StudentId })
                    .ToListAsync(ct)
                    .ContinueWith(t => t.Result.Select(x => (x.CompletedAt!.Value.Month, x.StudentId)).ToList(), ct);

            var presentByMonth = new HashSet<Guid>[12];
            for (var i = 0; i < 12; i++) presentByMonth[i] = new HashSet<Guid>();
            foreach (var (month, studentId) in completions) presentByMonth[month - 1].Add(studentId);

            var attendanceTrend = Enumerable.Range(0, 12)
                .Select(i => new
                {
                    month = MonthNames[i],
                    attendance = totalStudents == 0 ? 0.0 : Math.Round(100.0 * presentByMonth[i].Count / totalStudents, 2)
                })
                .ToList();

            var assessments = studentIds.Count == 0 ? new List<(int Month, double Score)>()
                : await _db.SessionAssessments.AsNoTracking()
                    .Join(_db.StudentIlsSessions.AsNoTracking(), a => a.SessionId, s => s.Id, (a, s) => new { a.Score, a.SubmittedAt, s.StudentId })
                    .Where(x => studentIds.Contains(x.StudentId))
                    .Select(x => new { x.Score, x.SubmittedAt })
                    .ToListAsync(ct)
                    .ContinueWith(t => t.Result.Select(x => (x.SubmittedAt.Month, (double)x.Score * 100.0)).ToList(), ct);

            var scoreSum = new double[12];
            var scoreCount = new int[12];
            foreach (var (month, score) in assessments) { scoreSum[month - 1] += score; scoreCount[month - 1]++; }
            var performanceTrend = Enumerable.Range(0, 12)
                .Select(i => new { month = MonthNames[i], average = scoreCount[i] == 0 ? 0.0 : Math.Round(scoreSum[i] / scoreCount[i], 2) })
                .ToList();

            return Ok(new { subjectData, attendanceTrend, performanceTrend });
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> Leaderboard(CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);

            if (studentIds.Count == 0) return Ok(new { entries = Array.Empty<object>() });

            var sessions = await _db.StudentIlsSessions.AsNoTracking()
                .Where(s => studentIds.Contains(s.StudentId))
                .Include(s => s.Assessment)
                .ToListAsync(ct);

            var names = (await _db.Users.AsNoTracking().Where(u => studentIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName }).ToListAsync(ct))
                .ToDictionary(x => x.Id, x => x.FullName);

            var entries = sessions
                .GroupBy(s => s.StudentId)
                .Select(g => new
                {
                    userId = g.Key,
                    studentName = names.TryGetValue(g.Key, out var n) ? n : "Unknown",
                    avgScore = g.Where(s => s.Assessment != null).Select(s => (double)s.Assessment!.Score * 100.0).DefaultIfEmpty(0.0).Average(),
                    experimentsCompleted = g.Count(s => s.CompletedAt != null),
                    badges = g.Count(s => s.BadgeAwarded)
                })
                .OrderByDescending(x => x.avgScore)
                .Select((x, i) => (object)new
                {
                    rank = i + 1,
                    x.userId,
                    x.studentName,
                    avgScore = Math.Round(x.avgScore, 2),
                    x.experimentsCompleted,
                    x.badges
                })
                .ToList();

            return Ok(new { entries });
        }

        [HttpGet("student/{studentId:guid}/report")]
        public async Task<IActionResult> StudentReport(Guid studentId, CancellationToken ct)
        {
            var teacherId = Me();
            var classroomIds = await GetTeacherClassroomIdsAsync(teacherId, ct);
            var studentIds = await GetStudentIdsAsync(classroomIds, ct);

            if (!studentIds.Contains(studentId))
                return StatusCode(StatusCodes.Status403Forbidden, new { error = true, code = "FORBIDDEN", message = "Student is not enrolled in any of your classrooms." });

            var student = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == studentId, ct);
            if (student == null) return NotFound(new { error = true, code = "NOT_FOUND", message = "Student not found." });

            var classroomName = "";
            var enrollRow = await _db.Enrollments.AsNoTracking()
                .Where(e => e.UserId == studentId && classroomIds.Contains(e.ClassroomId) && e.RoleInClass == ClassRole.Student)
                .Select(e => e.ClassroomId)
                .FirstOrDefaultAsync(ct);
            if (enrollRow != Guid.Empty)
            {
                var classroom = await _db.Classrooms.AsNoTracking().FirstOrDefaultAsync(c => c.Id == enrollRow, ct);
                classroomName = classroom?.Name ?? "";
            }

            var sessions = await _db.StudentIlsSessions.AsNoTracking()
                .Where(s => s.StudentId == studentId)
                .Include(s => s.Assessment)
                .Include(s => s.Ils)
                .ToListAsync(ct);

            var scores = sessions.Where(s => s.Assessment != null).Select(s => (double)s.Assessment!.Score * 100.0).ToList();
            var avgScore = scores.Count == 0 ? 0.0 : Math.Round(scores.Average(), 2);
            var experimentsCompleted = sessions.Count(s => s.CompletedAt != null);
            var experimentsInProgress = sessions.Count(s => s.CompletedAt == null);
            var badges = sessions.Count(s => s.BadgeAwarded);

            var launches = await _db.ExperimentLaunches.AsNoTracking()
                .Where(e => e.UserId == studentId)
                .SumAsync(e => (long)e.DurationSec, ct);
            var timeSpentMins = (int)(launches / 60);

            var scoreSum = new double[12];
            var scoreCount = new int[12];
            foreach (var s in sessions.Where(s => s.Assessment != null))
            {
                var m = s.Assessment!.SubmittedAt.Month - 1;
                scoreSum[m] += (double)s.Assessment.Score * 100.0;
                scoreCount[m]++;
            }
            var performanceTrend = Enumerable.Range(0, 12)
                .Select(i => new { month = MonthNames[i], average = scoreCount[i] == 0 ? 0.0 : Math.Round(scoreSum[i] / scoreCount[i], 2) })
                .ToList();

            var presentMonths = new HashSet<int>();
            foreach (var s in sessions.Where(s => s.CompletedAt != null)) presentMonths.Add(s.CompletedAt!.Value.Month - 1);
            var attendanceTrend = Enumerable.Range(0, 12)
                .Select(i => new { month = MonthNames[i], present = presentMonths.Contains(i) ? 1 : 0, absent = presentMonths.Contains(i) ? 0 : 1 })
                .ToList();

            var recentSessions = sessions
                .Where(s => s.CompletedAt != null)
                .OrderByDescending(s => s.CompletedAt)
                .Take(10)
                .Select(s => (object)new
                {
                    ilsTitle = s.Ils?.Title ?? "",
                    completedAt = s.CompletedAt,
                    score = s.Assessment != null ? Math.Round((double)s.Assessment.Score * 100.0, 2) : 0.0,
                    timeSpentMins = 0
                })
                .ToList();

            return Ok(new
            {
                studentId,
                studentName = student.FullName,
                classroomName,
                avgScore,
                experimentsCompleted,
                experimentsInProgress,
                badges,
                timeSpentMins,
                performanceTrend,
                attendanceTrend,
                recentSessions
            });
        }
    }
}
