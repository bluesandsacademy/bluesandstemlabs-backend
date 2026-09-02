using BlueSandsLMS.Common.DTOs.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/dashboard")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1Controller : ControllerBase
    {
        private readonly IGlobalAdminService _svc;
        private readonly IHardcodedGlobalAdminProvider _hardcoded;

        public GlobalAdminV1Controller(IGlobalAdminService svc, IHardcodedGlobalAdminProvider hardcoded) =>
            (_svc, _hardcoded) = (svc, hardcoded);

        [HttpGet("totals")]
        public async Task<ActionResult<GlobalAdminTotalsDto>> Totals(CancellationToken ct)
            => Ok(await _svc.GetTotalsAsync(ct));

        [HttpGet("prompt-totals")]
        public async Task<ActionResult<PromptTotalsDto>> PromptTotals(CancellationToken ct = default)
            => Ok(await _hardcoded.GetPromptTotalsAsync(ct));

        [HttpGet("growth")]
        public async Task<ActionResult<GrowthSeriesDto>> Growth([FromQuery] string metric = "users", [FromQuery] string period = "day", [FromQuery] int points = 30, CancellationToken ct = default)
            => Ok(await _svc.GetGrowthAsync(metric, period, points, ct));

        [HttpGet("geo-usage")]
        public async Task<ActionResult<GeoUsageDto>> GeoUsage(CancellationToken ct)
            => Ok(await _svc.GetGeoUsageAsync(ct));

        [HttpGet("schools")]
        public async Task<ActionResult<PagedResult<SchoolDetailDto>>> Schools(
            [FromQuery] string? q = null,
            [FromQuery] string? country = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
            => Ok(await _svc.GetSchoolsAsync(new SchoolQuery(q, country, isActive, page, pageSize), ct));
    }
}