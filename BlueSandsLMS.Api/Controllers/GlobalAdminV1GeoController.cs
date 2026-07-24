using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlueSandsLMS.Application.Services.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/dashboard/geo-advanced")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1GeoController : ControllerBase
    {
        private readonly IGlobalGeoService _svc;
        public GlobalAdminV1GeoController(IGlobalGeoService svc) => _svc = svc;


        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? scope, [FromQuery] string? country, [FromQuery] string? state, CancellationToken ct)
            => Ok(await _svc.GetAsync(scope ?? "country", country, state, ct));
    }
}
