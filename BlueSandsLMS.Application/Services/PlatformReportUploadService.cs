using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using ClosedXML.Excel;
using Google;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BlueSandsLMS.Application.Services;

public interface IPlatformReportUploadService
{
    Task<ExcelUploadResultDto> UploadAndProcessExcelAsync(IFormFile file);
}

public class PlatformReportUploadService : IPlatformReportUploadService
{
    private const string ExpectedSheetName = "Sheet1";
    private readonly BlueSandsLMSDbContext _context;
    private readonly ILogger<PlatformReportUploadService> _logger;

    public PlatformReportUploadService(BlueSandsLMSDbContext context, ILogger<PlatformReportUploadService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExcelUploadResultDto> UploadAndProcessExcelAsync(IFormFile file)
    {
        var result = new ExcelUploadResultDto();

        if (file == null || file.Length == 0)
        {
            result.Errors.Add("The uploaded file is empty or missing.");
            return result;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            result.Errors.Add("Invalid file format. Only Excel files (.xlsx, .xls) are supported.");
            return result;
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            using var workbook = new XLWorkbook(memoryStream);

            if (!workbook.Worksheets.TryGetWorksheet(ExpectedSheetName, out var worksheet))
            {
                result.Errors.Add($"Required worksheet '{ExpectedSheetName}' was not found in the workbook.");
                return result;
            }

            var rows = worksheet.RowsUsed().Skip(1); // Skip header row
            int rowNumber = 1;

            foreach (var row in rows)
            {
                rowNumber++;
                try
                {
                    var cell = row.Cell(1);
                    string reportDateString;

                    // Handle ClosedXML OADate (numeric excel serial date) or standard strings
                    if (cell.DataType == XLDataType.DateTime)
                    {
                        reportDateString = cell.GetDateTime().ToString("yyyy-MM-dd");
                    }
                    else if (cell.TryGetValue<double>(out double oaDate))
                    {
                        try
                        {
                            reportDateString = DateTime.FromOADate(oaDate).ToString("yyyy-MM-dd");
                        }
                        catch
                        {
                            reportDateString = cell.Value.ToString()?.Trim();
                        }
                    }
                    else
                    {
                        var rawVal = cell.Value.ToString()?.Trim();
                        if (double.TryParse(rawVal, out double parsedDouble))
                        {
                            try
                            {
                                reportDateString = DateTime.FromOADate(parsedDouble).ToString("yyyy-MM-dd");
                            }
                            catch
                            {
                                reportDateString = rawVal;
                            }
                        }
                        else
                        {
                            reportDateString = rawVal;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(reportDateString))
                    {
                        result.Errors.Add($"Row {rowNumber}: Invalid or missing date format.");
                        continue;
                    }

                    var record = new PlatformReportRecord
                    {
                        ReportDate = reportDateString,
                        NewStudentsCount = ParseNullableInt(row.Cell(2).Value.ToString()),
                        MonthlyActiveStudents = ParseNullableInt(row.Cell(3).Value.ToString()),
                        CompletedExperimentsCount = ParseNullableInt(row.Cell(4).Value.ToString()),
                        NewTeachersReached = ParseNullableInt(row.Cell(5).Value.ToString()),
                        VirtualExperimentsConducted = ParseNullableInt(row.Cell(6).Value.ToString()),
                        MonthlyRetentionRate = row.Cell(7).Value.ToString(),
                        NewK12SchoolsReached = ParseNullableInt(row.Cell(8).Value.ToString()),
                        TeacherFeedbackEffectiveness = row.Cell(9).Value.ToString(),
                        TeacherFeedbackLessonPlanning = row.Cell(10).Value.ToString(),
                        TeachersCreatingIlsCount = ParseNullableInt(row.Cell(11).Value.ToString()),
                        IlsCreatedCount = ParseNullableInt(row.Cell(12).Value.ToString()),
                        IlsInDraftCount = ParseNullableInt(row.Cell(13).Value.ToString()),
                        StudentPerformancePrior = row.Cell(14).Value.ToString(),
                        StudentPerformanceFollowing = row.Cell(15).Value.ToString()
                    };

                    _context.PlatformReportRecords.Add(record);
                    result.SuccessfulInserts++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row {RowNumber}", rowNumber);
                    result.Errors.Add($"Row {rowNumber}: An unexpected error occurred while parsing data.");
                }

                result.TotalRowsProcessed++;
            }

            if (result.Errors.Count > 0)
            {
                await transaction.RollbackAsync();
                result.Success = false;
                return result;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Critical error reading the Excel workbook.");
            result.Errors.Add($"Critical processing error: {ex.Message}");
            result.Success = false;
        }

        return result;
    }

    private int? ParseNullableInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-" || value == "--")
            return null;

        if (int.TryParse(value.Trim(), out var parsed))
            return parsed;

        return null;
    }
}
