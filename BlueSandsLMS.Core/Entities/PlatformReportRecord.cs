namespace BlueSandsLMS.Core.Entities;

using System;

public class PlatformReportRecord
{
    public int Id { get; set; }
    public string ReportDate { get; set; }
    public int? NewStudentsCount { get; set; }
    public int? MonthlyActiveStudents { get; set; }
    public int? CompletedExperimentsCount { get; set; }
    public int? NewTeachersReached { get; set; }
    public int? VirtualExperimentsConducted { get; set; }
    public string MonthlyRetentionRate { get; set; }
    public int? NewK12SchoolsReached { get; set; }
    public string TeacherFeedbackEffectiveness { get; set; }
    public string TeacherFeedbackLessonPlanning { get; set; }
    public int? TeachersCreatingIlsCount { get; set; }
    public int? IlsCreatedCount { get; set; }
    public int? IlsInDraftCount { get; set; }
    public string StudentPerformancePrior { get; set; }
    public string StudentPerformanceFollowing { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ExcelUploadResultDto
{
    public bool Success { get; set; }
    public int TotalRowsProcessed { get; set; }
    public int SuccessfulInserts { get; set; }
    public List<string> Errors { get; set; } = new();
}