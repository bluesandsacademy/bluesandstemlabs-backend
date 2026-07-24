using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlueSandsLMS.Common.Interfaces.Admin;
using BlueSandsLMS.Common.DTOs.Admin;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/reports")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1ReportsController : ControllerBase
    {
        private readonly IGlobalAdminService _svc;
        public GlobalAdminV1ReportsController(IGlobalAdminService svc) => _svc = svc;

        [HttpPost("export.csv")]
        public async Task<IActionResult> ExportCsv([FromBody] GlobalExportRequest req, CancellationToken ct)
        {
            var bytes = await _svc.ExportCsvAsync(req, ct);
            return File(bytes, "text/csv; charset=utf-8", $"{req.Type}-{req.Period}.csv");
        }
    }
}
