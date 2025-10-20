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
        [Authorize] // require login
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

            // If your Users.SchoolId is Guid (non-nullable), this will be Guid (not Guid?)
            var schoolId = await db.Users.AsNoTracking()
                .Where(u => u.Id == guid)
                .Select(u => u.SchoolId) // Guid (or Guid?)
                .FirstOrDefaultAsync();

            // If EF typed it as Guid? for you, normalize:
            var mySchoolId = schoolId; // keep as-is if Guid; if Guid? then use: (schoolId ?? Guid.Empty);

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
                    Subtotal = $"₦{p.Subtotal:N2}",
                    Vat = $"₦{p.Vat:N2}",
                    Amount = $"₦{p.Total:N2}",
                    Status = p.Status.ToString(),
                    p.PromoCode
                })
                .ToListAsync();

            return Ok(list);
        }


        /// <summary>
        /// Register a manual payment (cash/POS/bank) and activate user's subscription.
        /// Roles: Admin, GlobalAdmin, SchoolAdmin (only for users in their school).
        /// </summary>
       
        [HttpPost("register-payment")]
        [Authorize]
        public async Task<IActionResult> RegisterPayment([FromBody] RegisterPaymentRequest req)
        {
            try
            {
                var res = await _payments.RegisterManualAsync(req, User);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

  