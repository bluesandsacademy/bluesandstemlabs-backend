using System.Security.Claims;
using BlueSandsLMS.Application.Services;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _payments;
        public PaymentsController(IPaymentService payments) => _payments = payments;

        [HttpPost("initialize")]
        [HttpPost("initiate")]
        [Authorize]
        public async Task<IActionResult> Initialize([FromBody] InitPaymentRequest req)
            => Ok(await _payments.InitializeAsync(req, User));

        [HttpGet("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify([FromQuery] string reference)
            => Ok(await _payments.VerifyAsync(reference));

        [HttpPost("webhook/paystack")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();
            var sig = Request.Headers["x-paystack-signature"].ToString();
            await _payments.HandleWebhookAsync(raw, sig);
            return Ok();
        }

        [Authorize]
        [HttpGet("history")]
        public async Task<IActionResult> History([FromServices] BlueSandsLMSDbContext db)
        {
            var uid = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return Unauthorized();
            var guid = Guid.Parse(uid);


            var schoolId = await db.Users.AsNoTracking()
                .Where(u => u.Id == guid)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync();


            var mySchoolId = schoolId;


            string currency = "NGN";
            if (mySchoolId != null && mySchoolId != Guid.Empty)
            {
                currency = await db.Schools
                    .Where(s => s.Id == mySchoolId)
                    .Select(s => s.Currency)
                    .FirstOrDefaultAsync() ?? "NGN";
            }
            var sym = currency == "UGX" ? "USh" : "₦";

            var list = await db.Payments.AsNoTracking()
                .Where(p =>
                    p.UserId == guid ||
                    (mySchoolId != Guid.Empty && p.SchoolId == mySchoolId))
                .OrderByDescending(p => p.DateCreated)
                .Select(p => new
                {
                    p.Reference,
                    p.DateCreated,
                    Scope = p.SchoolId != Guid.Empty ? "School" : "Individual",
                    Source = p.RawResponse == "manual-registration" ? "Manual" : "Paystack",
                    Students = p.StudentsBilled,
                    PricePerStudent = p.PricePerStudent,
                    Subtotal = $"{sym}{p.Subtotal:N2}",
                    Vat = $"{sym}{p.Vat:N2}",
                    Amount = $"{sym}{p.Total:N2}",
                    Status = p.Status.ToString(),
                    p.PromoCode,
                    Currency = currency
                })
                .ToListAsync();

            return Ok(list);
        }



       
        [HttpPost("register-payment")]
[Authorize]
public async Task<IActionResult> RegisterPayment([FromBody] RegisterPaymentRequest req, [FromServices] ILogger<PaymentsController> logger)
{
    try
    {
        var res = await _payments.RegisterManualAsync(req, User);
        return Ok(res);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "RegisterPayment failed. Reference={Reference}, User={UserId}", req.Reference, req.UserId);
        return BadRequest(new { error = ex.Message });
    }
}

    }
}


