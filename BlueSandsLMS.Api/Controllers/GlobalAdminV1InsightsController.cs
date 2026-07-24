using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlueSandsLMS.Application.Services.Admin;
using BlueSandsLMS.Common.Interfaces.Admin;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/dashboard/insights")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1InsightsController : ControllerBase
    {
        private readonly IGlobalInsightsService _svc;
        public GlobalAdminV1InsightsController(IGlobalInsightsService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
            => Ok(await _svc.GetAsync(ct));
    }
}
