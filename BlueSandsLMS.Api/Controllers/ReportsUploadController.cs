using BlueSandsLMS.Application.Services;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BlueSandsLMS.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportsUploadController : ControllerBase
{
    private readonly IPlatformReportUploadService _uploadService;
    private readonly BlueSandsLMSDbContext _context;

    public ReportsUploadController(IPlatformReportUploadService uploadService, BlueSandsLMSDbContext context)
    {
        _uploadService = uploadService;
        _context = context;
    }

    // DTO returned by the GET endpoint matching requested schema
    private class ReportRecordDto
    {
        public int Id { get; set; }
        public string ReportDate { get; set; } = string.Empty; // formatted like "Jun-25"
        public int? NewStudentsCount { get; set; }
        public int? MonthlyActiveStudents { get; set; }
        public int? CompletedExperimentsCount { get; set; }
        public int? NewTeachersReached { get; set; }
        public int? VirtualExperimentsConducted { get; set; }
        public string MonthlyRetentionRate { get; set; } = string.Empty;
        public int? NewK12SchoolsReached { get; set; }
        public string? TeacherFeedbackEffectiveness { get; set; }
        public string? TeacherFeedbackLessonPlanning { get; set; }
        public int? TeachersCreatingIlsCount { get; set; }
        public int? IlsCreatedCount { get; set; }
        public int? IlsInDraftCount { get; set; }
        public string? StudentPerformancePrior { get; set; }
        public string? StudentPerformanceFollowing { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports()
    {
        var entities = await _context.PlatformReportRecords
            .OrderBy(r => r.ReportDate)
            .ToListAsync();

        var records = entities.Select(r => new ReportRecordDto
        {
            Id = r.Id,
            ReportDate = DateTime.TryParse(r.ReportDate, out var _dt) ? _dt.ToString("MMM-yy", CultureInfo.InvariantCulture) : (r.ReportDate ?? string.Empty),
            NewStudentsCount = r.NewStudentsCount,
            MonthlyActiveStudents = r.MonthlyActiveStudents,
            CompletedExperimentsCount = r.CompletedExperimentsCount,
            NewTeachersReached = r.NewTeachersReached,
            VirtualExperimentsConducted = r.VirtualExperimentsConducted,
            MonthlyRetentionRate = r.MonthlyRetentionRate,
            NewK12SchoolsReached = r.NewK12SchoolsReached,
            TeacherFeedbackEffectiveness = r.TeacherFeedbackEffectiveness,
            TeacherFeedbackLessonPlanning = r.TeacherFeedbackLessonPlanning,
            TeachersCreatingIlsCount = r.TeachersCreatingIlsCount,
            IlsCreatedCount = r.IlsCreatedCount,
            IlsInDraftCount = r.IlsInDraftCount,
            StudentPerformancePrior = r.StudentPerformancePrior,
            StudentPerformanceFollowing = r.StudentPerformanceFollowing,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(records);
    }

    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExcelUploadResultDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ExcelUploadResultDto))]
    public async Task<IActionResult> UploadReportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded or file is empty." });
        }

        var result = await _uploadService.UploadAndProcessExcelAsync(file);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
