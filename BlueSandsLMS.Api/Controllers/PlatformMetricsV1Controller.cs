using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/platform/v1")]
//[Authorize(Roles = "GlobalAdmin")]
public sealed class PlatformMetricsV1Controller : ControllerBase
{
    private readonly BlueSandsLMSDbContext _db;

    public PlatformMetricsV1Controller(BlueSandsLMSDbContext db) => _db = db;

    [HttpGet("overview")]
    public async Task<ActionResult<PlatformOverviewDto>> Overview(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var since30 = now.AddDays(-30);

        var totalStudents = await _db.Users.CountAsync(u => u.Role != null && u.Role.Name == "Student", ct);
        var totalSchools = await _db.Schools.CountAsync(ct);
        var totalSims = await _db.PhETSimulations.CountAsync(ct);

        var totalPayments = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => (decimal?)p.Total, ct) ?? 0m;

        var totalLabTimeSec = await _db.ExperimentLaunches.SumAsync(e => (long?)e.DurationSec, ct) ?? 0L;
        var totalLabMinutes = totalLabTimeSec / 60L;

        var totalExperiments = await _db.ExperimentLaunches.LongCountAsync(ct);

        var ilsCreated = await _db.InteractiveLearningSpaces.CountAsync(ct);

        var ilsDrafts = await _db.InteractiveLearningSpaces.CountAsync(i => i.Status == IlsStatus.Draft, ct);

        // Count distinct teacher creators for published ILS
        var teacherCreators = await _db.InteractiveLearningSpaces
            .Where(i => i.Status == IlsStatus.Published && i.CreatedByUser != null && i.CreatedByUser.Role != null && i.CreatedByUser.Role.Name == "Teacher")
            .Select(i => i.CreatedBy)
            .Distinct()
            .CountAsync(ct);

        string studentRoleId = "AE17F104-0EC3-47E3-9517-0E7E2C3BE8B0";
        string teacherRoleId = "D7C51101-D2A4-40D5-BB0A-BD97898CF847";

        var activeStudents30d = await _db.Users.CountAsync(u => u.Role != null && u.Role.Name == "Student" && u.LastLogin != null && u.LastLogin >= since30, ct);

        var maleUsers = await _db.Users
             .CountAsync(u => u.RoleId.ToString() == studentRoleId && u.Gender != null && u.Gender == "Male", ct);

        var femaleUsers = await _db.Users
            .CountAsync(u => u.RoleId.ToString() == studentRoleId && u.Gender != null && u.Gender == "Female", ct);


        var teachers = await _db.Users
           .CountAsync(u => u.RoleId.ToString() == teacherRoleId, ct);

        var totalUsers = maleUsers + femaleUsers + teachers;

        var dto = new PlatformOverviewDto
        {
            TotalPlatformUsers = totalUsers,
            TotalPlatformStudents = totalStudents,
            TotalSchoolsRegistered = totalSchools,
            TotalSimulations = totalSims,
            TotalPayments = totalPayments,
            TotalLabPracticeMinutes = totalLabMinutes,
            TotalExperimentAttempts = totalExperiments,
            IlsCreated = ilsCreated,
            TeachersCreatingIls = teacherCreators,
            IlsInDrafts = ilsDrafts,
            ActiveStudents30d = activeStudents30d,
            GeneratedAtUtc = now
        };

        return Ok(dto);
    }
}
