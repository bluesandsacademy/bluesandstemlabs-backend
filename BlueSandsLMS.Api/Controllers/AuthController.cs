using System;
using System.Net; // <- for WebUtility.UrlEncode
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Application.Services;
using BlueSandsLMS.Core.Entities;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IConfiguration _config;

        public AuthController(IAuthService auth, IConfiguration config)
        {
            _auth = auth;
            _config = config;
        }

        // -------------------------
        // Public student self-registration
        // -------------------------
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            try { return Ok(await _auth.RegisterAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // -------------------------
        // Public login
        // -------------------------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try { return Ok(await _auth.LoginAsync(dto)); }
            catch (Exception ex) { return Unauthorized(new { message = ex.Message }); }
        }

        // -------------------------
        // Public school admin registration
        // -------------------------
        [HttpPost("register/school")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterSchool([FromBody] RegisterSchoolDto dto)
        {
            try { return Ok(await _auth.RegisterSchoolAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // -------------------------
        // Email verification (link target)
        // -------------------------
       [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            // allow overriding in appsettings or env; fallback to production URL
            var frontendBase = _config["Frontend:BaseUrl"]?.TrimEnd('/')
                              ?? "https://app.bluesandstemlabs.com";

            try
            {
                await _auth.VerifyEmailAsync(token);

                var successUrl = $"{frontendBase}/auth/verify-success";
                return Redirect(successUrl); // 302 → frontend success page
            }
            catch (Exception ex)
            {
                var reason = WebUtility.UrlEncode(ex.Message);
                var failUrl = $"{frontendBase}/auth/verify-failed?reason={reason}";
                return Redirect(failUrl); // 302 → frontend fail page
            }
        }

        // -------------------------
        // Resend verification email
        // Accepts: { "email": "user@example.com" }
        // -------------------------
        public sealed class ResendVerificationRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        [HttpPost("resend-verification")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest req)
        {
            try
            {
                if (req is null || string.IsNullOrWhiteSpace(req.Email))
                    return BadRequest(new { message = "Email is required." });

                await _auth.ResendVerificationAsync(req.Email.Trim());
                return Ok(new { message = "Verification email resent." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // -------------------------
        // Current user profile (for dashboards)
        // -------------------------
        [Authorize]
[HttpGet("me")]
public async Task<IActionResult> Me([FromServices] BlueSandsLMSDbContext db)
{
    var sub = User.FindFirstValue("sub")
           ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue(ClaimTypes.Name);

    if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
        return Unauthorized();

    var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null) return Unauthorized();

    // Build base response matching AuthResponseDto structure
    var response = new AuthResponseDto
    {
        UserId = user.Id,
        FullName = user.FullName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        Role = user.Role?.Name ?? string.Empty,
        SchoolId = user.SchoolId,
        IsVerified = user.IsEmailVerified,
        Phone = user.Phone ?? string.Empty,
        Country = user.Country ?? string.Empty,
        Token = string.Empty // Not needed for /me endpoint
    };

    // 🔹 Fetch subscription data (same logic as auth/login)
    var schoolId = user.SchoolId ?? Guid.Empty;
    
    Subscription? subscription = null;
    
    if (schoolId != Guid.Empty)
    {
        // School user - lookup by SchoolId
        subscription = await db.Subscriptions
            .Where(s => s.SchoolId == schoolId && s.Active)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync();
    }
    else
    {
        // Individual user - lookup by UserId
        subscription = await db.Subscriptions
            .Where(s => s.UserId == userId && s.Active)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync();
    }

    if (subscription != null)
    {
        var end = subscription.EndsAt;
        var daysRemaining = Math.Max(0, (int)Math.Floor((end - DateTime.UtcNow).TotalDays));

        response.Subscription = new SubscriptionSummaryDto
        {
            Active = subscription.Active,
            StartsAt = subscription.StartsAt,
            EndsAt = subscription.EndsAt,
            StudentsCovered = subscription.StudentsCovered,
            PricePerStudent = subscription.PricePerStudent,
            LastPaymentReference = subscription.LastPaymentReference,
            DaysRemaining = daysRemaining
        };

        // Match tier by students covered
        var students = subscription.StudentsCovered;
        var tier = await db.PricingTiers
            .OrderBy(t => t.MinStudents)
            .FirstOrDefaultAsync(t => students >= t.MinStudents && students <= t.MaxStudents);

        if (tier != null)
        {
            response.CurrentTier = new TierSummaryDto
            {
                Id = tier.Id,
                TierName = tier.TierName,
                MinStudents = tier.MinStudents,
                MaxStudents = tier.MaxStudents,
                PricePerStudent = tier.PricePerStudent,
                IsMatch = true
            };
        }
    }

    return Ok(response);
}
    }
}
