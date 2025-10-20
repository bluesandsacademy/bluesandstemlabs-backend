using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs.Student;
using BlueSandsLMS.Common.Interfaces.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/student/v1")]
[Authorize(Roles = "Student,SchoolAdmin")] // allow SchoolAdmin to test
public class StudentV1Controller : ControllerBase
{
    private readonly IStudentDashboardService _svc;
    public StudentV1Controller(IStudentDashboardService svc) { _svc = svc; }

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

   
}
