using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;

namespace BlueSandsLMS.Application.Services
{
    public class PhETImportService : IPhETImportService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly ILogger<PhETImportService> _logger;

        public PhETImportService(BlueSandsLMSDbContext db, ILogger<PhETImportService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream, CancellationToken ct = default)
        {
            var tempPath = Path.GetTempFileName() + ".xlsx";
            try
            {
                await using (var fs = File.Create(tempPath))
                {
                    fileStream.Seek(0, SeekOrigin.Begin);
                    await fileStream.CopyToAsync(fs, ct);
                }
                return await ImportFromFileAsync(tempPath, ct);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch {  }
            }
        }

        public async Task<ImportResult> ImportFromFileAsync(string path, CancellationToken ct = default)
        {
            int imported = 0, updated = 0, skipped = 0;
            var errors = new List<string>();

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null)
                return new ImportResult(0, 0, 0, new[] { "No worksheet found" });

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastRow < 2)
                return new ImportResult(0, 0, 0, new[] { "No data rows found" });


            var headerRow = 2;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var headerByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= lastCol; c++)
            {
                var name = (ws.Cell(headerRow, c).GetValue<string>() ?? "").Trim();
                if (!string.IsNullOrEmpty(name) && !headerByName.ContainsKey(name))
                    headerByName.Add(name, c);
            }


            string[] required = {
                "Title","Type","# of Screens","Screen Names","Sim Page","Sim String","Teacher Tips Doc","PDF",
                "Physics","Math & Statistics","CHEMISTRY","EARTH & SPACE","BIOLOGY",
                "Low Grade Level","High Grade Level","Main Topics","Keywords","Description",
                "Sample Learning Goals","Translations","Published","Runnable Resource","CheerpJ Runnable","Filename"
            };

            var missing = required.Where(h => !headerByName.ContainsKey(h)).ToList();
            if (missing.Any())
                return new ImportResult(0, 0, 0, new[] { $"Missing headers: {string.Join(", ", missing)}" });

            string GetStr(int r, string h)
            {
                var v = ws.Cell(r, headerByName[h]).GetValue<string>();
                return string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();
            }


            string GetStrSafe(int r, string h, int maxLen)
            {
                var v = GetStr(r, h);
                if (string.IsNullOrEmpty(v)) return v;
                if (v.Length <= maxLen) return v;

                _logger.LogWarning("Truncating {Column} at row {Row} from {Original} to {Max} chars",
                    h, r, v.Length, maxLen);
                return v.Substring(0, maxLen);
            }

            bool GetBool(int r, string h)
            {
                var v = GetStr(r, h).ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(v) || v == "NONE") return false;


                if (v is "TRUE" or "YES" or "1" or "X") return true;


                var headerUpper = h.ToUpperInvariant().Replace(" ", "").Replace("&", "");
                var valueNorm = v.Replace(" ", "").Replace("&", "");
                return headerUpper.Contains(valueNorm) || valueNorm.Contains(headerUpper);
            }

            int? GetInt(int r, string h)
            {
                var s = GetStr(r, h);
                return int.TryParse(s, out var n) ? n : (int?)null;
            }

            static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

            static string Trunc(string? s, int max)
            {
                if (string.IsNullOrEmpty(s)) return string.Empty;
                return s.Length <= max ? s : s.Substring(0, max);
            }


            var existingList = await _db.PhETSimulations.ToListAsync(ct);
            var existing = new Dictionary<string, PhETSimulation>(StringComparer.OrdinalIgnoreCase);
            foreach (var sim in existingList)
            {
                var key = sim.Title.ToLower();
                if (!existing.ContainsKey(key))
                    existing[key] = sim;
                else
                    _logger.LogWarning("Duplicate title in database: {Title} (ID: {Id1} and {Id2})",
                        sim.Title, existing[key].Id, sim.Id);
            }


            var seenInSheet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


            for (int r = headerRow + 1; r <= lastRow; r++)
            {
                try
                {
                    var title = GetStr(r, "Title");
                    if (string.IsNullOrWhiteSpace(title)) { skipped++; continue; }


                    if (!seenInSheet.Add(title))
                    {
                        skipped++;
                        _logger.LogWarning("Duplicate title in sheet at row {Row}: {Title} (skipped second occurrence)", r, title);
                        continue;
                    }

                    var key = title.ToLowerInvariant();

                    var type = GetStr(r, "Type");
                    var numScreens = GetInt(r, "# of Screens");
                    var screenNames = GetStr(r, "Screen Names");
                    var simPage = GetStr(r, "Sim Page");
                    var simString = GetStr(r, "Sim String");
                    var teacherTips = GetStr(r, "Teacher Tips Doc");
                    var pdfUrl = GetStr(r, "PDF");
                    var physics = GetBool(r, "Physics");
                    var mathStats = GetBool(r, "Math & Statistics");
                    var chemistry = GetBool(r, "CHEMISTRY");
                    var earthSpace = GetBool(r, "EARTH & SPACE");
                    var biology = GetBool(r, "BIOLOGY");
                    var lowGrade = GetStr(r, "Low Grade Level");
                    var highGrade = GetStr(r, "High Grade Level");
                    var mainTopics = GetStrSafe(r, "Main Topics", 500);
                    var keywords = GetStrSafe(r, "Keywords", 500);
                    var description = GetStrSafe(r, "Description", 500);
                    var learningGoalsNew = GetStrSafe(r, "Sample Learning Goals", 2000);
                    var translations = GetStrSafe(r, "Translations", 500);
                    var published = GetStr(r, "Published");
                    var runnableResource = GetStr(r, "Runnable Resource");
                    var cheerpjRunnable = GetStr(r, "CheerpJ Runnable");
                    var filename = GetStr(r, "Filename");

                    if (existing.TryGetValue(key, out var row))
                    {

                        row.Type = NullIfEmpty(type);
                        row.NumberOfScreens = numScreens;
                        row.ScreenNames = NullIfEmpty(screenNames);
                        row.SimPage = NullIfEmpty(simPage);
                        row.SimString = NullIfEmpty(simString);
                        row.TeacherTipsDoc = NullIfEmpty(teacherTips);
                        row.PdfUrl = NullIfEmpty(pdfUrl);
                        row.Physics = physics;
                        row.MathStatistics = mathStats;
                        row.Chemistry = chemistry;
                        row.EarthSpace = earthSpace;
                        row.Biology = biology;
                        row.LowGradeLevel = NullIfEmpty(lowGrade);
                        row.HighGradeLevel = NullIfEmpty(highGrade);
                        row.MainTopics = NullIfEmpty(mainTopics);
                        row.Keywords = NullIfEmpty(keywords);
                        row.Description = NullIfEmpty(description);
                        row.SampleLearningGoals = NullIfEmpty(learningGoalsNew);
                        row.Translations = NullIfEmpty(translations);
                        row.Published = NullIfEmpty(published);
                        row.RunnableResource = NullIfEmpty(runnableResource);
                        row.CheerpJRunnable = NullIfEmpty(cheerpjRunnable);
                        row.Filename = NullIfEmpty(filename);

                        row.LastUpdated = DateTime.UtcNow;
                        updated++;
                    }
                    else
                    {
                        var rowNew = new PhETSimulation
                        {
                            Id = Guid.NewGuid(),
                            Title = title,
                            Type = NullIfEmpty(type),
                            NumberOfScreens = numScreens,
                            ScreenNames = NullIfEmpty(screenNames),
                            SimPage = NullIfEmpty(simPage),
                            SimString = NullIfEmpty(simString),
                            TeacherTipsDoc = NullIfEmpty(teacherTips),
                            PdfUrl = NullIfEmpty(pdfUrl),
                            Physics = physics,
                            MathStatistics = mathStats,
                            Chemistry = chemistry,
                            EarthSpace = earthSpace,
                            Biology = biology,
                            LowGradeLevel = NullIfEmpty(lowGrade),
                            HighGradeLevel = NullIfEmpty(highGrade),
                            MainTopics = NullIfEmpty(mainTopics),
                            Keywords = NullIfEmpty(keywords),
                            Description = NullIfEmpty(description),
                            SampleLearningGoals = NullIfEmpty(learningGoalsNew),
                            Translations = NullIfEmpty(translations),
                            Published = NullIfEmpty(published),
                            RunnableResource = NullIfEmpty(runnableResource),
                            CheerpJRunnable = NullIfEmpty(cheerpjRunnable),
                            Filename = NullIfEmpty(filename),

                            IsActive = true,
                            DateCreated = DateTime.UtcNow
                        };
                        _db.PhETSimulations.Add(rowNew);
                        existing[key] = rowNew;
                        imported++;
                    }

                    if ((imported + updated) % 200 == 0)
                    {
                        await _db.SaveChangesAsync(ct);
                        _logger.LogInformation("PhET import progress: {Count}", imported + updated);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {r}: {ex.Message}");
                    _logger.LogError(ex, "Error on row {Row}", r);
                }
            }

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {

                errors.Add($"Save failed (final save): {ex.GetBaseException().Message}");
                _logger.LogError(ex, "Final SaveChanges failed");
            }

            return new ImportResult(imported, updated, skipped, errors);
        }
    }
}