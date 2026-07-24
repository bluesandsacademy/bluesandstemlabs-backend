using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/admin/phet")]
    [Authorize(Roles = "GlobalAdmin,SchoolAdmin")]
    public class PhETAdminController : ControllerBase
{
    private readonly IPhETImportService _importService;
    private readonly IPhETSeedDataService _seedService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PhETAdminController> _logger;

    public PhETAdminController(
        IPhETImportService importService,
        IPhETSeedDataService seedService,
        IWebHostEnvironment env,
        ILogger<PhETAdminController> logger)
    {
        _importService = importService;
        _seedService = seedService;
        _env = env;
        _logger = logger;
        
    }
        

     [HttpPost("import")]
[RequestSizeLimit(50_000_000)]
[Consumes("multipart/form-data")]
public async Task<ActionResult<ImportResult>> ImportSimulations(IFormFile file, CancellationToken ct)
{
    if (file is null || file.Length == 0)
        return BadRequest(new { message = "No file uploaded" });

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not ".xlsx" and not ".xls" and not ".csv")
        return BadRequest(new { message = "Only .xlsx, .xls, .csv supported" });


    var uploadDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "phet");
    Directory.CreateDirectory(uploadDir);

    var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    var safeBase = Path.GetFileNameWithoutExtension(file.FileName);
    var savedPath = Path.Combine(uploadDir, $"{safeBase}_{stamp}{ext}");

    await using (var fs = System.IO.File.Create(savedPath))
        await file.CopyToAsync(fs, ct);

    var result = await _importService.ImportFromFileAsync(savedPath, ct);
    return Ok(result);
}

        

        [HttpPost("seed-data")]
        public async Task<ActionResult<SeedResult>> GenerateSeedData(
            [FromQuery] int studentCount = 10,
            [FromQuery] int experimentsPerStudent = 5,
            CancellationToken ct = default)
        {
            if (studentCount < 1 || studentCount > 1000)
                return BadRequest(new { message = "Student count: 1-1000" });
            
            if (experimentsPerStudent < 1 || experimentsPerStudent > 50)
                return BadRequest(new { message = "Experiments per student: 1-50" });
            
            try
            {
                var result = await _seedService.GenerateSeedDataAsync(studentCount, experimentsPerStudent, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating seed data");
                return StatusCode(500, new { message = "Seed failed", error = ex.Message });
            }
        }
    }
}