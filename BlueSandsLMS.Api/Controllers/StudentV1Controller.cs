using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Student;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/student/v1")]
[Authorize(Roles = "Student,SchoolAdmin")]
public class StudentV1Controller : ControllerBase
{
    private readonly IStudentDashboardService _svc;
    private readonly BlueSandsLMSDbContext _db;

    public StudentV1Controller(IStudentDashboardService svc, BlueSandsLMSDbContext db)
    {
        _svc = svc;
        _db  = db;
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var id))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            throw new InvalidOperationException("Missing or invalid user id claim.");
        }
        return id;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<StudentOverviewDto>> Overview(CancellationToken ct)
    {
        var userId = CurrentUserId();
        var dto = await _svc.GetOverviewAsync(userId, ct);
        return Ok(dto);
    }

    [HttpGet("attempts")]
    public async Task<ActionResult<IReadOnlyList<StudentAttemptDto>>> Attempts(
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var userId = CurrentUserId();
        var items = await _svc.GetRecentQuizAttemptsAsync(userId, take, ct);
        return Ok(items);
    }

    [HttpGet("experiments")]
    public async Task<ActionResult<IReadOnlyList<StudentExperimentDto>>> Experiments(
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var userId = CurrentUserId();
        var items = await _svc.GetRecentExperimentsAsync(userId, take, ct);
        return Ok(items);
    }

    [HttpGet("badges")]
    public async Task<ActionResult<IReadOnlyList<StudentBadgeDto>>> Badges(CancellationToken ct)
    {
        var userId = CurrentUserId();
        var items = await _svc.GetBadgesAsync(userId, ct);
        return Ok(items);
    }

    private static readonly string[] MonthNames =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    [HttpGet("time-spent")]
    public async Task<IActionResult> TimeSpent(CancellationToken ct)
    {
        var userId = CurrentUserId();

        var labSec = await _db.ExperimentLaunches.AsNoTracking()
            .Where(e => e.UserId == userId)
            .SumAsync(e => (long)e.DurationSec, ct);

        var quizSec = await _db.QuizAttempts.AsNoTracking()
            .Where(q => q.UserId == userId && q.CompletedAt != null)
            .Select(q => new { q.StartedAt, CompletedAt = q.CompletedAt!.Value })
            .ToListAsync(ct);
        var quizSeconds = quizSec.Sum(q => Math.Max(0, (q.CompletedAt - q.StartedAt).TotalSeconds));

        var data = new[]
        {
            new { name = "Lab", hours = Math.Round(labSec / 3600.0, 2) },
            new { name = "Quiz", hours = Math.Round(quizSeconds / 3600.0, 2) },
            new { name = "Discussion", hours = 0.0 },
            new { name = "Reading", hours = 0.0 }
        };

        return Ok(new { data });
    }

    [HttpGet("performance")]
    public async Task<IActionResult> Performance(CancellationToken ct)
    {
        var userId = CurrentUserId();

        var rows = await _db.SessionAssessments.AsNoTracking()
            .Join(_db.StudentIlsSessions.AsNoTracking(), a => a.SessionId, s => s.Id, (a, s) => new { a.Score, a.SubmittedAt, s.StudentId })
            .Where(x => x.StudentId == userId)
            .Select(x => new { x.Score, x.SubmittedAt })
            .ToListAsync(ct);

        var scoreSum = new double[12];
        var scoreCount = new int[12];
        foreach (var r in rows)
        {
            var m = r.SubmittedAt.Month - 1;
            scoreSum[m] += (double)r.Score * 100.0;
            scoreCount[m]++;
        }

        var trends = Enumerable.Range(0, 12)
            .Select(i => new { month = MonthNames[i], average = scoreCount[i] == 0 ? 0.0 : Math.Round(scoreSum[i] / scoreCount[i], 2) })
            .ToList();

        return Ok(new { trends });
    }


    [HttpGet("assessments/summary")]
    public async Task<ActionResult<StudentAssessmentSummaryDto>> AssessmentSummary(CancellationToken ct)
    {
        var userId = CurrentUserId();

        var quizStats = await _db.QuizAttempts
            .AsNoTracking()
            .Where(q => q.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Attempted = g.Count(),
                Passed    = g.Count(q => q.Passed),
                AvgScore  = g.Average(q => (decimal?)q.Score0to1),
                Recent    = g.Max(q => q.CompletedAt)
            })
            .FirstOrDefaultAsync(ct);


        var ilsStats = await _db.StudentIlsSessions
            .AsNoTracking()
            .Where(s => s.StudentId == userId && s.Assessment != null)
            .Select(s => new
            {
                Score = (decimal?)s.Assessment!.Score
            })
            .ToListAsync(ct);

        var ilsCompleted = ilsStats.Count;
        var ilsAvg = ilsCompleted == 0
            ? 0.0
            : Math.Round((double)((ilsStats.Where(x => x.Score.HasValue).Average(x => x.Score) ?? 0m) * 100m), 1);

        return Ok(new StudentAssessmentSummaryDto(
            QuizzesAttempted:       quizStats?.Attempted ?? 0,
            QuizzesPassed:          quizStats?.Passed ?? 0,
            AvgQuizScorePercent:    Math.Round((double)((quizStats?.AvgScore ?? 0m) * 100m), 1),
            MostRecentQuizDate:     quizStats?.Recent,
            IlsAssessmentsCompleted: ilsCompleted,
            AvgIlsScorePercent:     ilsAvg
        ));
    }
}
