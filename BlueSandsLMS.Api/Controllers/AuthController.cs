using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using BlueSandsLMS.Api.Infrastructure;
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

        private IActionResult Error(int statusCode, string code, string message)
        {
            return StatusCode(statusCode, ApiErrorFactory.Create(statusCode, code, message));
        }


        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var origin = Request.Headers["Origin"].FirstOrDefault();
            try { return Ok(await _auth.RegisterAsync(dto, origin)); }
            catch (UserAlreadyExistsException ex) { return Error(StatusCodes.Status409Conflict, "USER_EXISTS", ex.Message); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try { return Ok(await _auth.LoginAsync(dto)); }
            catch (Exception ex) { return Unauthorized(new { message = ex.Message }); }
        }

        [HttpPost("login/teacher")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginTeacher([FromBody] LoginDto dto)
        {
            try { return Ok(await _auth.LoginForRoleAsync(dto, "Teacher", 8)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
            catch (Exception ex) { return Unauthorized(new { message = ex.Message }); }
        }

        [HttpPost("login/student")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginStudent([FromBody] LoginDto dto)
        {
            try { return Ok(await _auth.LoginForRoleAsync(dto, "Student", 4)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
            catch (Exception ex) { return Unauthorized(new { message = ex.Message }); }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            try { return Ok(await _auth.RefreshTokenAsync(dto.RefreshToken)); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }


        [HttpPost("register/school")]
        [HttpPost("register-school")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterSchool([FromBody] RegisterSchoolDto dto)
        {
            var origin = Request.Headers["Origin"].FirstOrDefault();
            try { return Ok(await _auth.RegisterSchoolAsync(dto, origin)); }
            catch (UserAlreadyExistsException ex) { return Error(StatusCodes.Status409Conflict, "USER_EXISTS", ex.Message); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }


        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, [FromQuery] string? site = null)
        {
            var brand = BlueSandsLMS.Application.Emails.SiteBrandResolver.ResolveByKey(site, _config);

            try
            {
                await _auth.VerifyEmailAsync(token);
                return Redirect($"{brand.FrontendBaseUrl}/auth/verify-success");
            }
            catch (Exception ex)
            {
                var reason = WebUtility.UrlEncode(ex.Message);
                return Redirect($"{brand.FrontendBaseUrl}/auth/verify-failed?reason={reason}");
            }
        }
[HttpPost("forgot-password")]
[AllowAnonymous]
public async Task<IActionResult> ForgotPassword([FromBody] RequestPasswordResetDto dto)
{
    try
    {
        var origin = Request.Headers["Origin"].FirstOrDefault();
        await _auth.RequestPasswordResetAsync(dto.Email, origin);
        return Ok(new
        {
            message = "If an account exists with this email, you will receive password reset instructions."
        });
    }
    catch
    {

        return Ok(new
        {
            message = "If an account exists with this email, you will receive password reset instructions."
        });
    }
}

[HttpPost("reset-password")]
[AllowAnonymous]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
{
    try
    {
        var origin = Request.Headers["Origin"].FirstOrDefault();
        await _auth.ResetPasswordAsync(dto.Token, dto.NewPassword, origin);
        return Ok(new { message = "Password reset successful. You can now log in with your new password." });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

[HttpPost("change-password")]
[Authorize]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
{
    try
    {
        var sub = User.FindFirstValue("sub") 
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();
        
        var origin = Request.Headers["Origin"].FirstOrDefault();
        await _auth.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword, origin);
        return Ok(new { message = "Password changed successfully." });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

        [HttpPost("google")]
        [HttpPost("google-signin")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleSignIn([FromBody] GoogleSignInDto dto, CancellationToken ct)
        {
            try { return Ok(await _auth.GoogleSignInAsync(dto.IdToken, ct)); }
            catch (UserAlreadyExistsException ex) { return Error(StatusCodes.Status409Conflict, "USER_EXISTS", ex.Message); }
            catch (UnauthorizedAccessException ex) { return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", ex.Message); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }


        [HttpPost("google-signup")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleSignUp([FromBody] GoogleSignUpDto dto, CancellationToken ct)
        {
            var origin = Request.Headers["Origin"].FirstOrDefault();
            try { return Ok(await _auth.GoogleSignUpAsync(dto, origin, ct)); }
            catch (UserAlreadyExistsException ex) { return Error(StatusCodes.Status409Conflict, "USER_EXISTS", ex.Message); }
            catch (UnauthorizedAccessException ex) { return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", ex.Message); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }


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

                var origin = Request.Headers["Origin"].FirstOrDefault();
                await _auth.ResendVerificationAsync(req.Email.Trim(), origin);
                return Ok(new { message = "Verification email resent." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


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


    string? schoolName = null;
    string schoolCurrency = "NGN";
    if (user.SchoolId.HasValue && user.SchoolId.Value != Guid.Empty)
    {
        var school = await db.Schools
            .Where(s => s.Id == user.SchoolId.Value)
            .Select(s => new { s.Name, s.Currency })
            .FirstOrDefaultAsync();
        schoolName = school?.Name;
        schoolCurrency = school?.Currency ?? "NGN";
    }

    var response = new AuthResponseDto
    {
        UserId = user.Id,
        FullName = user.FullName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        Role = user.Role?.Name ?? string.Empty,
        SchoolId = user.SchoolId,
        SchoolName = schoolName,
        SchoolCurrency = schoolCurrency,
        IsVerified = user.IsEmailVerified,
        Phone = user.Phone ?? string.Empty,
        Country = user.Country ?? string.Empty,
        Token = string.Empty
    };

    var schoolId = user.SchoolId ?? Guid.Empty;
    
    Subscription? subscription = null;
    
    if (schoolId != Guid.Empty)
    {
        subscription = await db.Subscriptions
            .Where(s => s.SchoolId == schoolId && s.Active)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync();
    }
    else
    {
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

[Authorize]
[HttpPut("me")]
public async Task<IActionResult> UpdateMe(
    [FromBody] UpdateProfileDto request,
    [FromServices] BlueSandsLMSDbContext db,
    CancellationToken ct)
{
    var sub = User.FindFirstValue("sub")
           ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue(ClaimTypes.Name);

    if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
        return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    if (user == null)
        return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "User not found.");

    if (request.FullName is not null) user.FullName = request.FullName.Trim();
    if (request.Phone is not null) user.Phone = request.Phone.Trim();
    if (request.Country is not null) user.Country = request.Country.Trim();
    if (request.Gender is not null) user.Gender = request.Gender.Trim();

    await db.SaveChangesAsync(ct);

    return Ok(new
    {
        id = user.Id,
        fullName = user.FullName,
        phone = user.Phone,
        country = user.Country,
        gender = user.Gender ?? string.Empty,
        email = user.Email
    });
}
    }
}
