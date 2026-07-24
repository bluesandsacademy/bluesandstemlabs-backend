using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/globaladmin/v1/settings")]
    [Authorize(Roles = "GlobalAdmin")]
    public sealed class GlobalAdminV1SettingsController : ControllerBase
    {
        private readonly IConfiguration _config;
        public GlobalAdminV1SettingsController(IConfiguration config) => _config = config;

        [HttpGet]
        public IActionResult Get()
        {

            var langs = _config.GetSection("App:Languages").Get<string[]>() ?? new[] { "en" };
            var currency = _config["App:Currency"] ?? "NGN";
            var regions = _config.GetSection("App:Regions").Get<string[]>() ?? new string[0];

            return Ok(new { languages = langs, currency, regions });
        }
    }
}
