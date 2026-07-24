using System.Security.Claims;
using BlueSandsLMS.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/praxilabs")]
    [Authorize]
    public sealed class PraxiLabsController : ControllerBase
    {
        private readonly IPraxiLabsService _praxiLabs;
        private readonly ILogger<PraxiLabsController> _logger;

        public PraxiLabsController(IPraxiLabsService praxiLabs, ILogger<PraxiLabsController> logger)
        {
            _praxiLabs = praxiLabs;
            _logger = logger;
        }

        [HttpGet("branches")]
        [Authorize(Roles = "Teacher,GlobalAdmin,SchoolAdmin")]
        public async Task<IActionResult> GetBranches(CancellationToken ct)
        {
            try
            {
                var branches = await _praxiLabs.GetBranchesAsync(ct);
                return Ok(branches);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "PraxiLabs branches request failed");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = true,
                    code = "PRAXILABS_UNAVAILABLE",
                    message = "PraxiLabs service is currently unavailable. Please try again later."
                });
            }
        }

        [HttpGet("experiments")]
        [Authorize(Roles = "Teacher,GlobalAdmin,SchoolAdmin")]
        public async Task<IActionResult> GetExperiments(
            [FromQuery] string? scienceId = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;

            try
            {
                var result = await _praxiLabs.GetExperimentsAsync(scienceId, search, page, ct);
                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "PraxiLabs experiments request failed");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = true,
                    code = "PRAXILABS_UNAVAILABLE",
                    message = "PraxiLabs service is currently unavailable. Please try again later."
                });
            }
        }

        [HttpGet("catalog-url")]
        [Authorize(Roles = "Teacher,Student,GlobalAdmin,SchoolAdmin")]
        public async Task<IActionResult> GetCatalogUrl(CancellationToken ct)
        {
            var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var url = await _praxiLabs.GetCatalogUrlAsync(userId, ct);
                if (string.IsNullOrEmpty(url))
                    return StatusCode(StatusCodes.Status502BadGateway, new
                    {
                        error = true,
                        code = "PRAXILABS_NO_URL",
                        message = "PraxiLabs did not return a catalog URL."
                    });

                return Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {

                _logger.LogWarning("PraxiLabs catalog error for user {UserId}: {Message}", userId, ex.Message);
                return StatusCode(StatusCodes.Status404NotFound, new
                {
                    error = true,
                    code = "PRAXILABS_USER_NOT_FOUND",
                    message = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "PraxiLabs catalog-url request failed");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = true,
                    code = "PRAXILABS_UNAVAILABLE",
                    message = "PraxiLabs service is currently unavailable. Please try again later."
                });
            }
        }
    }
}
