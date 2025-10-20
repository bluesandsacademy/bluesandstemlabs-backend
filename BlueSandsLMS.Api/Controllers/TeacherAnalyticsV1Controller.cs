using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Application.Services.Teacher;
using BlueSandsLMS.Common.Interfaces.Teacher;
using BlueSandsLMS.Common.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BlueSandsLMS.Api.Controllers;

[ApiController]
[Route("api/teacher/v1/analytics")]
[Authorize(Roles = "Teacher,SchoolAdmin,GlobalAdmin")]
public sealed class TeacherAnalyticsV1Controller : ControllerBase
{
    private readonly ITeacherAnalyticsService _svc;
    public TeacherAnalyticsV1Controller(ITeacherAnalyticsService svc) => _svc = svc;

    private Guid Me()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var id))
        {
            Response.StatusCode = 401;
            throw new InvalidOperationException("Missing or invalid user id claim.");
        }
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

    [HttpGet("overview")]
    public async Task<ActionResult<TeacherOverviewDto>> Overview([FromQuery] Guid? classroomId, [FromQuery] string? subject, [FromQuery] string? range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (f, t) = Range(range, from, to);
        var res = await _svc.OverviewAsync(Me(), classroomId, subject, f, t, ct);
        return Ok(res);
    }

    [HttpGet("engagement")]
    public async Task<ActionResult<TeacherEngagementDto>> Engagement([FromQuery] Guid? classroomId, [FromQuery] string? subject, [FromQuery] string? range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (f, t) = Range(range, from, to);
        var res = await _svc.EngagementAsync(Me(), classroomId, subject, f, t, ct);
        return Ok(res);
    }

    [HttpGet("performance")]
    public async Task<ActionResult<TeacherPerformanceDto>> Performance([FromQuery] Guid? classroomId, [FromQuery] string? subject, [FromQuery] string? range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (f, t) = Range(range, from, to);
        var res = await _svc.PerformanceAsync(Me(), classroomId, subject, f, t, ct);
        return Ok(res);
    }

    [HttpGet("assignments")]
    public async Task<ActionResult<TeacherAssignmentsDto>> Assignments([FromQuery] Guid? classroomId, [FromQuery] string? subject, [FromQuery] string? range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (f, t) = Range(range, from, to);
        var res = await _svc.AssignmentsAsync(Me(), classroomId, subject, f, t, ct);
        return Ok(res);
    }

    [HttpGet("attendance")]
    public async Task<ActionResult<TeacherAttendanceDto>> Attendance([FromQuery] Guid? classroomId, [FromQuery] string? subject, [FromQuery] string? range, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var (f, t) = Range(range, from, to);
        var res = await _svc.AttendanceAsync(Me(), classroomId, subject, f, t, ct);
        return Ok(res);
    }
}
