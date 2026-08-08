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
                    s.Title.ToLower().Contains(searchLower) ||
                    (s.Description != null && s.Description.ToLower().Contains(searchLower)) ||
                    (s.Keywords != null && s.Keywords.ToLower().Contains(searchLower)) ||
                    (s.MainTopics != null && s.MainTopics.ToLower().Contains(searchLower))
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
                    Type = s.Type,
                    NumberOfScreens = s.NumberOfScreens,
                    ScreenNames = s.ScreenNames,
                    SimPage = s.SimPage,
                    SimString = s.SimString,
                    TeacherTipsDoc = s.TeacherTipsDoc,
                    PdfUrl = s.PdfUrl,
                    Physics = s.Physics,
                    MathStatistics = s.MathStatistics,
                    Chemistry = s.Chemistry,
                    EarthSpace = s.EarthSpace,
                    Biology = s.Biology,
                    LowGradeLevel = s.LowGradeLevel,
                    HighGradeLevel = s.HighGradeLevel,
                    MainTopics = s.MainTopics,
                    Keywords = s.Keywords,
                    Description = s.Description,
                    SampleLearningGoals = s.SampleLearningGoals,
                    Translations = s.Translations,
                    Published = s.Published,
                    RunnableResource = s.RunnableResource,
                    CheerpJRunnable = s.CheerpJRunnable,
                    Filename = s.Filename
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
                    Type = s.Type,
                    NumberOfScreens = s.NumberOfScreens,
                    ScreenNames = s.ScreenNames,
                    SimPage = s.SimPage,
                    SimString = s.SimString,
                    TeacherTipsDoc = s.TeacherTipsDoc,
                    PdfUrl = s.PdfUrl,
                    Physics = s.Physics,
                    MathStatistics = s.MathStatistics,
                    Chemistry = s.Chemistry,
                    EarthSpace = s.EarthSpace,
                    Biology = s.Biology,
                    LowGradeLevel = s.LowGradeLevel,
                    HighGradeLevel = s.HighGradeLevel,
                    MainTopics = s.MainTopics,
                    Keywords = s.Keywords,
                    Description = s.Description,
                    SampleLearningGoals = s.SampleLearningGoals,
                    Translations = s.Translations,
                    Published = s.Published,
                    RunnableResource = s.RunnableResource,
                    CheerpJRunnable = s.CheerpJRunnable,
                    Filename = s.Filename
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
            var stats = new
            {
                TotalSimulations = await _db.PhETSimulations.Where(s => s.IsActive).CountAsync(ct),
                Subjects = new[]
                {
                    new {
                        Name = "Physics",
                        Count = await _db.PhETSimulations.Where(s => s.IsActive && s.Physics).CountAsync(ct)
                    },
                    new {
                        Name = "Chemistry",
                        Count = await _db.PhETSimulations.Where(s => s.IsActive && s.Chemistry).CountAsync(ct)
                    },
                    new {
                        Name = "Math",
                        Count = await _db.PhETSimulations.Where(s => s.IsActive && s.MathStatistics).CountAsync(ct)
                    },
                    new {
                        Name = "Biology",
                        Count = await _db.PhETSimulations.Where(s => s.IsActive && s.Biology).CountAsync(ct)
                    },
                    new {
                        Name = "EarthSpace",
                        Count = await _db.PhETSimulations.Where(s => s.IsActive && s.EarthSpace).CountAsync(ct)
                    }
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
                .Where(s => s.IsActive && s.MainTopics != null)
                .Select(s => s.MainTopics)
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

