using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlueSandsLMS.Common.Interfaces.Admin;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/support")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1SupportController : ControllerBase
    {
        private readonly IGlobalAdminService _svc;
        public GlobalAdminV1SupportController(IGlobalAdminService svc) => _svc = svc;

        [HttpGet("overview")]
        public async Task<IActionResult> Overview(CancellationToken ct)
            => Ok(await _svc.GetSupportOverviewAsync(ct));

        [HttpGet("messages")]
        public async Task<IActionResult> Messages([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
            => Ok(await _svc.GetSupportMessagesAsync(page, pageSize, ct));
    }
}
