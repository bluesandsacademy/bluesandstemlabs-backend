using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlueSandsLMS.Common.Interfaces.Admin;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/billing")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1BillingController : ControllerBase
    {
        private readonly IGlobalAdminService _svc;
        public GlobalAdminV1BillingController(IGlobalAdminService svc) => _svc = svc;

        [HttpGet("payments")]
        public async Task<IActionResult> Payments([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
            => Ok(await _svc.GetPaymentsAsync(page, pageSize, ct));

        [HttpGet("subscriptions")]
        public async Task<IActionResult> Subscriptions([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
            => Ok(await _svc.GetSubscriptionsAsync(page, pageSize, ct));

        [HttpGet("revenue")]
        public async Task<IActionResult> Revenue(CancellationToken ct = default)
            => Ok(await _svc.GetRevenueBreakdownAsync(ct));
    }
}
