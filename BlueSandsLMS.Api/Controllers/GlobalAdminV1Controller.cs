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
        public GlobalAdminV1Controller(IGlobalAdminService svc) => _svc = svc;

        [HttpGet("totals")]
        public async Task<ActionResult<GlobalAdminTotalsDto>> Totals(CancellationToken ct)
            => Ok(await _svc.GetTotalsAsync(ct));

        [HttpGet("growth")]
        public async Task<ActionResult<GrowthSeriesDto>> Growth([FromQuery] string metric = "users", [FromQuery] string period = "day", [FromQuery] int points = 30, CancellationToken ct = default)
            => Ok(await _svc.GetGrowthAsync(metric, period, points, ct));

        [HttpGet("geo-usage")]
        public async Task<ActionResult<GeoUsageDto>> GeoUsage(CancellationToken ct)
            => Ok(await _svc.GetGeoUsageAsync(ct));
    }
}
