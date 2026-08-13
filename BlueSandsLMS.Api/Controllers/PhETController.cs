using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/phet")]
    public class PhETController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IExcelUploadService _uploadService;
        private readonly ILogger<PhETController> _logger;

        public PhETController(BlueSandsLMSDbContext db, IExcelUploadService uploadService,
            ILogger<PhETController> logger)
        {
            _db = db;
            _uploadService = uploadService;
            _logger = logger;
        }


        [HttpGet("simulations")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<PhETSimulationDto>>> GetSimulations(
            [FromQuery] bool? physics = null,
            [FromQuery] bool? chemistry = null,
            [FromQuery] bool? math = null,
            [FromQuery] bool? biology = null,
            [FromQuery] bool? earthSpace = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (pageSize > 100) pageSize = 100;

            var query = _db.PhETSimulations.Where(s => s.IsActive);


            if (physics == true) query = query.Where(s => s.Physics);
            if (chemistry == true) query = query.Where(s => s.Chemistry);
            if (math == true) query = query.Where(s => s.MathStatistics);
            if (biology == true) query = query.Where(s => s.Biology);
            if (earthSpace == true) query = query.Where(s => s.EarthSpace);


            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(s =>
                    s.Title.Contains(searchLower) ||
                    (s.Description != null && s.Description.Contains(searchLower)) ||
                    (s.Keywords != null && s.Keywords.Contains(searchLower)) ||
                    (s.Topic != null && s.Topic.Contains(searchLower)) ||
                    (s.MainTopics != null && s.MainTopics.Contains(searchLower))
                );
            }

            var total = await query.CountAsync(ct);

            var items = await query
     .OrderBy(s => s.Title)
     .Skip((page - 1) * pageSize)
     .Take(pageSize)
     .Select(s => new PhETSimulationDto
     {
         Id = s.Id,
         Title = s.Title,
         SimulationUrl = s.SimulationUrl,
         ThumbnailUrl = s.ThumbnailUrl,
         Topic = s.Topic,
         Description = s.Description,
         LearningGoals = s.LearningGoals,
         GradeLevel = s.GradeLevel,
         Standards = s.Standards,
         Keywords = s.Keywords,
         IsActive = s.IsActive,
         DateCreated = s.DateCreated,
         LastUpdated = s.LastUpdated,
         Type = s.Type,
         NumberOfScreens = s.NumberOfScreens,
         ScreenNames = s.ScreenNames,
         SimPage = s.SimPage,
         SimString = s.SimString,
         TeacherTipsDoc = s.TeacherTipsDoc,
         PdfUrl = s.PdfUrl,
         RunnableResource = s.RunnableResource,
         CheerpJRunnable = s.CheerpJRunnable,
         Filename = s.Filename,
         Physics = s.Physics,
         MathStatistics = s.MathStatistics,
         Chemistry = s.Chemistry,
         EarthSpace = s.EarthSpace,
         Biology = s.Biology,
         LowGradeLevel = s.LowGradeLevel,
         HighGradeLevel = s.HighGradeLevel,
         MainTopics = s.MainTopics,
         SampleLearningGoals = s.SampleLearningGoals,
         Translations = s.Translations,
         Published = s.Published,
         IsFree = s.IsFree
     })
     .ToListAsync(ct);

            return Ok(new PagedResult<PhETSimulationDto>(items, total, page, pageSize));
        }

        [HttpGet("simulations/{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<PhETSimulationDto>> GetSimulation(Guid id, CancellationToken ct)
        {
            var sim = await _db.PhETSimulations
                .Where(s => s.Id == id && s.IsActive)
                .Select(s => new PhETSimulationDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    SimulationUrl = s.SimulationUrl,
                    ThumbnailUrl = s.ThumbnailUrl,
                    Topic = s.Topic,
                    Description = s.Description,
                    LearningGoals = s.LearningGoals,
                    GradeLevel = s.GradeLevel,
                    Standards = s.Standards,
                    Keywords = s.Keywords,
                    IsActive = s.IsActive,
                    DateCreated = s.DateCreated,
                    LastUpdated = s.LastUpdated,
                    Type = s.Type,
                    NumberOfScreens = s.NumberOfScreens,
                    ScreenNames = s.ScreenNames,
                    SimPage = s.SimPage,
                    SimString = s.SimString,
                    TeacherTipsDoc = s.TeacherTipsDoc,
                    PdfUrl = s.PdfUrl,
                    RunnableResource = s.RunnableResource,
                    CheerpJRunnable = s.CheerpJRunnable,
                    Filename = s.Filename,
                    Physics = s.Physics,
                    MathStatistics = s.MathStatistics,
                    Chemistry = s.Chemistry,
                    EarthSpace = s.EarthSpace,
                    Biology = s.Biology,
                    LowGradeLevel = s.LowGradeLevel,
                    HighGradeLevel = s.HighGradeLevel,
                    MainTopics = s.MainTopics,
                    SampleLearningGoals = s.SampleLearningGoals,
                    Translations = s.Translations,
                    Published = s.Published,
                    IsFree = s.IsFree
                })
                .FirstOrDefaultAsync(ct);

            if (sim == null)
                return NotFound();

            return Ok(sim);
        }


        [HttpGet("statistics")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetStatistics(CancellationToken ct)
        {
            var subjectCounts = await _db.PhETSimulations
                .Where(s => s.IsActive)
                .GroupBy(s => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Physics = g.Count(s => s.Physics),
                    Chemistry = g.Count(s => s.Chemistry),
                    Math = g.Count(s => s.MathStatistics),
                    Biology = g.Count(s => s.Biology),
                    EarthSpace = g.Count(s => s.EarthSpace)
                })
                .FirstOrDefaultAsync(ct);

            var stats = new
            {
                TotalSimulations = subjectCounts?.Total ?? 0,
                Subjects = new[]
                {
            new { Name = "Physics", Count = subjectCounts?.Physics ?? 0 },
            new { Name = "Chemistry", Count = subjectCounts?.Chemistry ?? 0 },
            new { Name = "Math", Count = subjectCounts?.Math ?? 0 },
            new { Name = "Biology", Count = subjectCounts?.Biology ?? 0 },
            new { Name = "EarthSpace", Count = subjectCounts?.EarthSpace ?? 0 }
        }
            };

            return Ok(stats);
        }


        [HttpGet("main-topics")]
        [AllowAnonymous]
        public async Task<ActionResult<System.Collections.Generic.List<string>>> GetMainTopics(
            [FromQuery] int limit = 50,
            CancellationToken ct = default)
        {
            var allTopics = await _db.PhETSimulations
                .Where(s => s.IsActive && s.Topic != null)
                .Select(s => s.Topic)
                .ToListAsync(ct);

            var topicCounts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var topicString in allTopics)
            {
                if (string.IsNullOrWhiteSpace(topicString)) continue;

                var topics = topicString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t));

                foreach (var topic in topics)
                {
                    if (topicCounts.ContainsKey(topic))
                        topicCounts[topic]++;
                    else
                        topicCounts[topic] = 1;
                }
            }

            var popularTopics = topicCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(limit)
                .Select(kvp => kvp.Key)
                .ToList();

            return Ok(popularTopics);
        }

        /// <summary>
        /// Upload an Excel (.xlsx) file containing PhET simulation data.
        /// Columns are matched by header name (order does not matter).
        /// </summary>
        [HttpPost("upload-excel")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
        public async Task<IActionResult> UploadExcel(IFormFile file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { error = true, message = "No file uploaded." });

            var ext = Path.GetExtension(file.FileName);
            if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".xls", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = true, message = "Only .xlsx or .xls files are supported." });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var count = await _uploadService.UploadPhETExcelAsync(stream);

                _logger.LogInformation("PhET Excel upload complete: {Count} records inserted", count);

                return Ok(new
                {
                    success = true,
                    message = $"{count} simulation(s) imported successfully.",
                    count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PhET Excel upload failed");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = true,
                    message = "Import failed: " + ex.Message
                });
            }
        }
    }



}

