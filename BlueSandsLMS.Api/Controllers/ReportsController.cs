using System.Security.Claims;
using System.Text;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public sealed class ReportsController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;

        public ReportsController(BlueSandsLMSDbContext db) => _db = db;

        private sealed record TeacherClassPerformanceRow(
            Guid ClassroomId,
            string ClassroomName,
            string Subject,
            int StudentCount,
            int AssignmentsCreated,
            int SubmissionsReceived,
            double AssignmentCompletionRatePercent,
            double AvgQuizScorePercent,
            int ExperimentLaunches);

        private sealed record TeacherReportResponse(
            int Classes,
            int TotalStudents,
            int ActiveStudents,
            int AssignmentsCreated,
            int SubmissionsReceived,
            double AssignmentCompletionRatePercent,
            double AvgQuizScorePercent,
            int ExperimentLaunches,
            double AvgExperimentLaunchesPerStudent,
            IReadOnlyList<TeacherClassPerformanceRow> ClassPerformance);

        private sealed record StudentQuizScoreRow(
            Guid AttemptId,
            string Subject,
            string QuizCode,
            double ScorePercent,
            bool Passed,
            DateTime ActivityAt);

        private sealed record StudentAssignmentHistoryRow(
            Guid AssignmentId,
            string Title,
            Guid ClassroomId,
            string ClassroomName,
            string Type,
            DateTime CreatedAt,
            DateTime? DueAt,
            string Status,
            double? ScorePercent,
            DateTime? SubmittedAt,
            DateTime? GradedAt);

        private sealed record StudentExperimentRow(
            Guid ExperimentLaunchId,
            string ExperimentName,
            Guid? ClassroomId,
            string? ClassroomName,
            bool Completed,
            DateTime StartedAt);

        private sealed record StudentReportResponse(
            int EnrolledClasses,
            int QuizzesAttempted,
            int QuizzesPassed,
            double AvgQuizScorePercent,
            int ExperimentsCompleted,
            int AssignmentsSubmitted,
            double PersonalProgressPercent,
            IReadOnlyList<StudentQuizScoreRow> RecentQuizScores,
            IReadOnlyList<StudentAssignmentHistoryRow> AssignmentHistory,
            IReadOnlyList<StudentExperimentRow> ExperimentHistory);

        private sealed record SchoolTopSimulationRow(string Simulation, int Runs);

        private sealed record SchoolReportResponse(
            int TotalStudents,
            int TotalTeachers,
            int TotalClasses,
            int SimulationUsage,
            double QuizPassRatePercent,
            IReadOnlyList<SchoolTopSimulationRow> TopSimulations);

        private Guid Me()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(sub!);
        }

        private Guid? MySchoolId()
        {
            var schoolId = User.FindFirstValue("SchoolId");
            return Guid.TryParse(schoolId, out var id) ? id : null;
        }

        private static string Csv(params object?[] values)
            => string.Join(",", values.Select(value =>
            {
                if (value is null) return "";
                var text = value switch
                {
                    DateTime dt => dt.ToString("O"),
                    DateTimeOffset dto => dto.ToString("O"),
                    _ => value.ToString() ?? ""
                };

                return text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r')
                    ? $"\"{text.Replace("\"", "\"\"")}\""
                    : text;
            }));

        private async Task<List<Guid>> TeacherClassIdsAsync(Guid teacherId, CancellationToken ct)
        {
            var fromEnrollments = await _db.Enrollments.AsNoTracking()
                .Where(x => x.UserId == teacherId && x.RoleInClass == ClassRole.Teacher)
                .Select(x => x.ClassroomId)
                .ToListAsync(ct);

            var fromAssignments = await _db.ClassroomTeachers.AsNoTracking()
                .Where(x => x.TeacherUserId == teacherId)
                .Select(x => x.ClassroomId)
                .ToListAsync(ct);

            return fromEnrollments.Union(fromAssignments).Distinct().ToList();
        }

        private async Task<TeacherReportResponse> BuildTeacherReportAsync(Guid teacherId, CancellationToken ct)
        {
            var classIds = await TeacherClassIdsAsync(teacherId, ct);
            if (classIds.Count == 0)
            {
                return new TeacherReportResponse(0, 0, 0, 0, 0, 0, 0, 0, 0, Array.Empty<TeacherClassPerformanceRow>());
            }

            var classes = await _db.Classrooms.AsNoTracking()
                .Where(x => classIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name, x.Subject })
                .ToListAsync(ct);

            var studentCounts = await _db.Enrollments.AsNoTracking()
                .Where(x => classIds.Contains(x.ClassroomId) && x.RoleInClass == ClassRole.Student)
                .GroupBy(x => x.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
                .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

            var assignmentsByClass = await _db.Assignments.AsNoTracking()
                .Where(x => classIds.Contains(x.ClassroomId))
                .GroupBy(x => x.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

            var submissionsByClass = await _db.Submissions.AsNoTracking()
                .Where(x => x.SubmittedAt != null && x.Assignment != null && classIds.Contains(x.Assignment.ClassroomId))
                .GroupBy(x => x.Assignment!.ClassroomId)
                .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

            var quizAveragesByClass = await _db.QuizAttempts.AsNoTracking()
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                .GroupBy(x => x.ClassroomId!.Value)
                .Select(g => new { ClassroomId = g.Key, Avg = g.Average(x => (double)x.Score0to1) * 100.0 })
                .ToDictionaryAsync(x => x.ClassroomId, x => x.Avg, ct);

            var experimentLaunchesByClass = await _db.ExperimentLaunches.AsNoTracking()
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                .GroupBy(x => x.ClassroomId!.Value)
                .Select(g => new { ClassroomId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassroomId, x => x.Count, ct);

            var activeStudentIds = (await _db.ExperimentLaunches.AsNoTracking()
                    .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                    .Select(x => x.UserId)
                    .ToListAsync(ct))
                .Union(await _db.QuizAttempts.AsNoTracking()
                    .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                    .Select(x => x.UserId)
                    .ToListAsync(ct))
                .Union(await _db.Submissions.AsNoTracking()
                    .Where(x => x.Assignment != null && classIds.Contains(x.Assignment.ClassroomId) && x.SubmittedAt != null)
                    .Select(x => x.StudentId)
                    .ToListAsync(ct))
                .Distinct()
                .ToList();

            var classPerformance = classes
                .Select(x =>
                {
                    var studentCount = studentCounts.GetValueOrDefault(x.Id);
                    var assignmentsCreated = assignmentsByClass.GetValueOrDefault(x.Id);
                    var submissionsReceived = submissionsByClass.GetValueOrDefault(x.Id);
                    var completionRate = assignmentsCreated == 0 || studentCount == 0
                        ? 0
                        : Math.Round(100.0 * submissionsReceived / (assignmentsCreated * studentCount), 1);

                    return new TeacherClassPerformanceRow(
                        x.Id,
                        x.Name,
                        x.Subject,
                        studentCount,
                        assignmentsCreated,
                        submissionsReceived,
                        completionRate,
                        Math.Round(quizAveragesByClass.GetValueOrDefault(x.Id), 2),
                        experimentLaunchesByClass.GetValueOrDefault(x.Id));
                })
                .OrderByDescending(x => x.AvgQuizScorePercent)
                .ThenBy(x => x.ClassroomName)
                .ToList();

            var totalStudents = studentCounts.Values.Sum();
            var assignmentsCreatedTotal = assignmentsByClass.Values.Sum();
            var submissionsReceivedTotal = submissionsByClass.Values.Sum();
            var experimentLaunchesTotal = experimentLaunchesByClass.Values.Sum();

            var avgQuizScorePercent = await _db.QuizAttempts.AsNoTracking()
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                .Select(x => (double?)x.Score0to1)
                .AverageAsync(ct) ?? 0;

            var assignmentCompletionRate = assignmentsCreatedTotal == 0 || totalStudents == 0
                ? 0
                : Math.Round(100.0 * submissionsReceivedTotal / (assignmentsCreatedTotal * totalStudents), 1);

            var avgExperimentLaunchesPerStudent = totalStudents == 0
                ? 0
                : Math.Round((double)experimentLaunchesTotal / totalStudents, 2);

            return new TeacherReportResponse(
                classPerformance.Count,
                totalStudents,
                activeStudentIds.Count,
                assignmentsCreatedTotal,
                submissionsReceivedTotal,
                assignmentCompletionRate,
                Math.Round(avgQuizScorePercent * 100.0, 2),
                experimentLaunchesTotal,
                avgExperimentLaunchesPerStudent,
                classPerformance);
        }

        [HttpGet("teacher")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Teacher(CancellationToken ct)
            => Ok(await BuildTeacherReportAsync(Me(), ct));

        [HttpGet("teacher/export/csv")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> TeacherCsv(CancellationToken ct)
        {
            var report = await BuildTeacherReportAsync(Me(), ct);
            var sb = new StringBuilder();
            sb.AppendLine(Csv("Metric", "Value"));
            sb.AppendLine(Csv("Classes", report.Classes));
            sb.AppendLine(Csv("Total Students", report.TotalStudents));
            sb.AppendLine(Csv("Active Students", report.ActiveStudents));
            sb.AppendLine(Csv("Assignments Created", report.AssignmentsCreated));
            sb.AppendLine(Csv("Submissions Received", report.SubmissionsReceived));
            sb.AppendLine(Csv("Assignment Completion Rate %", report.AssignmentCompletionRatePercent));
            sb.AppendLine(Csv("Avg Quiz Score %", report.AvgQuizScorePercent));
            sb.AppendLine(Csv("Experiment Launches", report.ExperimentLaunches));
            sb.AppendLine(Csv("Avg Experiment Launches Per Student", report.AvgExperimentLaunchesPerStudent));
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "teacher-report.csv");
        }

        private async Task<StudentReportResponse> BuildStudentReportAsync(Guid studentId, CancellationToken ct)
        {
            var enrolledClasses = await _db.Enrollments.AsNoTracking()
                .Where(x => x.UserId == studentId && x.RoleInClass == ClassRole.Student)
                .Select(x => x.ClassroomId)
                .Distinct()
                .ToListAsync(ct);

            var quizAttempts = await _db.QuizAttempts.AsNoTracking()
                .Where(x => x.UserId == studentId)
                .OrderByDescending(x => x.CompletedAt ?? x.StartedAt)
                .Select(x => new StudentQuizScoreRow(
                    x.Id,
                    x.Subject,
                    x.QuizCode,
                    Math.Round((double)x.Score0to1 * 100.0, 2),
                    x.Passed,
                    x.CompletedAt ?? x.StartedAt))
                .Take(10)
                .ToListAsync(ct);

            var quizStats = await _db.QuizAttempts.AsNoTracking()
                .Where(x => x.UserId == studentId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Attempts = g.Count(),
                    Passed = g.Count(x => x.Passed),
                    Avg = g.Average(x => (double?)x.Score0to1)
                })
                .FirstOrDefaultAsync(ct);

            var assignmentHistory = await _db.Assignments.AsNoTracking()
                .Where(x => enrolledClasses.Contains(x.ClassroomId))
                .GroupJoin(
                    _db.Submissions.AsNoTracking().Where(x => x.StudentId == studentId),
                    assignment => assignment.Id,
                    submission => submission.AssignmentId,
                    (assignment, submissions) => new { assignment, submission = submissions.OrderByDescending(x => x.SubmittedAt ?? x.GradedAt).FirstOrDefault() })
                .OrderByDescending(x => x.assignment.DueAt ?? x.assignment.CreatedAt)
                .Select(x => new StudentAssignmentHistoryRow(
                    x.assignment.Id,
                    x.assignment.Title,
                    x.assignment.ClassroomId,
                    x.assignment.Classroom != null ? x.assignment.Classroom.Name : string.Empty,
                    x.assignment.Type.ToString(),
                    x.assignment.CreatedAt,
                    x.assignment.DueAt,
                    (x.submission != null ? x.submission.Status : SubmissionStatus.Pending).ToString(),
                    x.submission != null && x.submission.Score0to1 != null ? Math.Round((double)(x.submission.Score0to1.Value * 100m), 2) : null,
                    x.submission != null ? x.submission.SubmittedAt : null,
                    x.submission != null ? x.submission.GradedAt : null))
                .Take(20)
                .ToListAsync(ct);

            var experimentHistory = await _db.ExperimentLaunches.AsNoTracking()
                .Where(x => x.UserId == studentId)
                .OrderByDescending(x => x.StartedAt)
                .Select(x => new StudentExperimentRow(
                    x.Id,
                    x.ExperimentName,
                    x.ClassroomId,
                    x.Classroom != null ? x.Classroom.Name : null,
                    x.Completed,
                    x.StartedAt))
                .Take(10)
                .ToListAsync(ct);

            var assignmentsSubmitted = assignmentHistory.Count(x => x.SubmittedAt != null);
            var experimentsCompleted = experimentHistory.Count(x => x.Completed);
            var personalProgressPercent = assignmentHistory.Count == 0
                ? 0
                : Math.Round(100.0 * assignmentsSubmitted / assignmentHistory.Count, 1);

            return new StudentReportResponse(
                enrolledClasses.Count,
                quizStats?.Attempts ?? 0,
                quizStats?.Passed ?? 0,
                Math.Round((quizStats?.Avg ?? 0) * 100.0, 2),
                experimentsCompleted,
                assignmentsSubmitted,
                personalProgressPercent,
                quizAttempts,
                assignmentHistory,
                experimentHistory);
        }

        [HttpGet("student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Student(CancellationToken ct)
            => Ok(await BuildStudentReportAsync(Me(), ct));

        [HttpGet("student/export/csv")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentCsv(CancellationToken ct)
        {
            var report = await BuildStudentReportAsync(Me(), ct);
            var sb = new StringBuilder();
            sb.AppendLine(Csv("Metric", "Value"));
            sb.AppendLine(Csv("Enrolled Classes", report.EnrolledClasses));
            sb.AppendLine(Csv("Quizzes Attempted", report.QuizzesAttempted));
            sb.AppendLine(Csv("Quizzes Passed", report.QuizzesPassed));
            sb.AppendLine(Csv("Avg Quiz Score %", report.AvgQuizScorePercent));
            sb.AppendLine(Csv("Experiments Completed", report.ExperimentsCompleted));
            sb.AppendLine(Csv("Assignments Submitted", report.AssignmentsSubmitted));
            sb.AppendLine(Csv("Personal Progress %", report.PersonalProgressPercent));
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "student-report.csv");
        }

        private async Task<SchoolReportResponse> BuildSchoolReportAsync(Guid schoolId, CancellationToken ct)
        {
            var totalStudents = await _db.Users.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.Role != null && x.Role.Name == "Student")
                .CountAsync(ct);

            var totalTeachers = await _db.Users.AsNoTracking()
                .Where(x => x.SchoolId == schoolId && x.Role != null && x.Role.Name == "Teacher")
                .CountAsync(ct);

            var classIds = await _db.Classrooms.AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var simulationUsage = await _db.ExperimentLaunches.AsNoTracking()
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                .CountAsync(ct);

            var quizAgg = await _db.QuizAttempts.AsNoTracking()
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                .GroupBy(_ => 1)
                .Select(g => new { Total = g.Count(), Passed = g.Count(x => x.Passed) })
                .FirstOrDefaultAsync(ct);

            var topSimulationRows = await _db.ExperimentLaunches.AsNoTracking()
                .Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value))
                .GroupBy(x => x.ExperimentName)
                .Select(g => new { Simulation = g.Key, Runs = g.Count() })
                .OrderByDescending(x => x.Runs)
                .Take(5)
                .ToListAsync(ct);

            var topSimulations = topSimulationRows
                .Select(x => new SchoolTopSimulationRow(x.Simulation, x.Runs))
                .ToList();

            var passRate = (quizAgg?.Total ?? 0) == 0
                ? 0
                : Math.Round(100.0 * quizAgg!.Passed / quizAgg.Total, 1);

            return new SchoolReportResponse(
                totalStudents,
                totalTeachers,
                classIds.Count,
                simulationUsage,
                passRate,
                topSimulations);
        }

        [HttpGet("school")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> School(CancellationToken ct)
        {
            var schoolId = MySchoolId();
            if (schoolId is null) return BadRequest(new { message = "SchoolId missing in token." });
            return Ok(await BuildSchoolReportAsync(schoolId.Value, ct));
        }

        [HttpGet("school/export/csv")]
        [Authorize(Roles = "SchoolAdmin")]
        public async Task<IActionResult> SchoolCsv(CancellationToken ct)
        {
            var schoolId = MySchoolId();
            if (schoolId is null) return BadRequest(new { message = "SchoolId missing in token." });

            var report = await BuildSchoolReportAsync(schoolId.Value, ct);
            var sb = new StringBuilder();
            sb.AppendLine(Csv("Metric", "Value"));
            sb.AppendLine(Csv("Total Students", report.TotalStudents));
            sb.AppendLine(Csv("Total Teachers", report.TotalTeachers));
            sb.AppendLine(Csv("Total Classes", report.TotalClasses));
            sb.AppendLine(Csv("Simulation Usage", report.SimulationUsage));
            sb.AppendLine(Csv("Quiz Pass Rate %", report.QuizPassRatePercent));
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "school-report.csv");
        }

        [HttpGet("analytics")]
        [Authorize(Roles = "SchoolAdmin,GlobalAdmin")]
        public async Task<IActionResult> Analytics([FromQuery] int days = 30, CancellationToken ct = default)
        {
            days = Math.Clamp(days <= 0 ? 30 : days, 7, 120);
            var since = DateTime.UtcNow.Date.AddDays(-days);

            List<Guid>? classIds = null;
            if (User.IsInRole("SchoolAdmin"))
            {
                var schoolId = MySchoolId();
                if (schoolId is null) return BadRequest(new { message = "SchoolId missing in token." });

                classIds = await _db.Classrooms.AsNoTracking()
                    .Where(x => x.SchoolId == schoolId.Value)
                    .Select(x => x.Id)
                    .ToListAsync(ct);
            }

            var launches = _db.ExperimentLaunches.AsNoTracking()
                .Where(x => x.StartedAt >= since);

            if (classIds != null)
            {
                launches = launches.Where(x => x.ClassroomId != null && classIds.Contains(x.ClassroomId.Value));
            }

            var dailyActiveUsers = await launches
                .GroupBy(x => x.StartedAt.Date)
                .Select(g => new { date = g.Key, activeUsers = g.Select(x => x.UserId).Distinct().Count() })
                .OrderBy(x => x.date)
                .ToListAsync(ct);

            var mostUsedSimulations = await launches
                .GroupBy(x => x.ExperimentName)
                .Select(g => new { simulation = g.Key, runs = g.Count() })
                .OrderByDescending(x => x.runs)
                .Take(10)
                .ToListAsync(ct);

            var peakUsageHours = await launches
                .GroupBy(x => x.StartedAt.Hour)
                .Select(g => new { hour = g.Key, count = g.Count() })
                .OrderBy(x => x.hour)
                .ToListAsync(ct);

            return Ok(new { dailyActiveUsers, mostUsedSimulations, peakUsageHours });
        }
    }
}
