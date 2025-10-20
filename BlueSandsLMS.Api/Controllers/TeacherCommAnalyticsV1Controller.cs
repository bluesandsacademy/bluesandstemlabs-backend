// BlueSandsLMS.Api/Controllers/TeacherCommAnalyticsV1Controller.cs
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Common.Interfaces.Teacher;
using BlueSandsLMS.Common.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers;

[ApiController]
[Route("api/teacher/v1/analytics")]
[Authorize(Roles = "Teacher,SchoolAdmin,GlobalAdmin")]
public sealed class TeacherCommAnalyticsV1Controller : ControllerBase
{
    private readonly ITeacherCommAnalyticsService _svc;
    public TeacherCommAnalyticsV1Controller(ITeacherCommAnalyticsService svc) => _svc = svc;

    private Guid Me()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var id)) throw new InvalidOperationException("Missing or invalid user id claim.");
        return id;
    }

    private static (DateTime from, DateTime to) Range(string? range, DateTime? from, DateTime? to)
    {
        var t = to ?? DateTime.UtcNow;
        var f = from ?? range switch
        {
            "7d" => t.AddDays(-7),
            "30d" => t.AddDays(-30),
            "term" => t.AddDays(-90),
            _ => t.AddDays(-7)
        };
        return (f, t);
    }

    [HttpGet("comm")]
    public async Task<ActionResult<TeacherCommOverviewDto>> Comm([FromQuery] Guid? classroomId, [FromQuery] string? range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (f, t) = Range(range, from, to);
        var res = await _svc.CommOverviewAsync(Me(), classroomId, f, t, ct);
        return Ok(res);
    }

    [HttpGet("forum")]
    public async Task<ActionResult<TeacherForumOverviewDto>> Forum([FromQuery] Guid? classroomId, [FromQuery] string? range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (f, t) = Range(range, from, to);
        var res = await _svc.ForumOverviewAsync(Me(), classroomId, f, t, ct);
        return Ok(res);
    }
}
