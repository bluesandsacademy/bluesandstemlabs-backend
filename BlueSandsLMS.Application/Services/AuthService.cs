using BlueSandsLMS.Application.Emails;
using BlueSandsLMS.Application.Services;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BlueSandsLMS.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IConfiguration _config;
        private readonly IEmailService _email;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(
            BlueSandsLMSDbContext db,
            IConfiguration config,
            IEmailService email,
            ILogger<AuthService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _config = config;
            _email = email;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }


        private async Task<string> GetRoleNameAsync(Guid roleId)
        {
            return await _db.Roles
                .Where(r => r.Id == roleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        private static string FirstNameOf(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "there";
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : "there";
        }

        private static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "school";
            var s = new string(input.ToLower().Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
            s = string.Join("-", s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return string.IsNullOrWhiteSpace(s) ? "school" : s;
        }

        private static DateTime? ParseDob(string? dob)
        {
            if (string.IsNullOrWhiteSpace(dob)) return null;
            if (DateTime.TryParse(dob, out var dt)) return dt.Date;
            var formats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "MM/dd/yyyy" };
            foreach (var f in formats)
                if (DateTime.TryParseExact(dob, f, null, System.Globalization.DateTimeStyles.None, out dt)) return dt.Date;
            return null;
        }

        private string GetJwtSecret() =>
            _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");

        private string GetRefreshSecret() =>
            _config["Jwt:RefreshSecret"] ?? GetJwtSecret();

        private static string NormalizeEmail(string? email) =>
            (email ?? string.Empty).Trim().ToLowerInvariant();

        private async Task EnsureEmailAvailableAsync(string? email, CancellationToken ct = default)
        {
            var normalizedEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                return;

            var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);
            if (exists)
                throw new UserAlreadyExistsException("An account with this email address already exists. Please login or use a different email.");
        }

        private TimeSpan AccessTokenTtlForRole(string roleName)
        {
            var minutes = _config.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 120;
            return TimeSpan.FromMinutes(minutes);
        }

        private string BuildRefreshToken(User user, string roleName, DateTime expiresAtUtc)
        {
            var secret = GetRefreshSecret();
            var issuer = _config["Jwt:Issuer"] ?? string.Empty;
            var audience = _config["Jwt:Audience"] ?? string.Empty;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("role", roleName),        // Use literal "role"
                new Claim("token_type", "refresh")
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = issuer,
                Audience = audience,
                Expires = expiresAtUtc,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(descriptor);
            return handler.WriteToken(token);
        }

        private async Task<(bool applied, string? message)> TryApplyCouponAsync(string? couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
                return (false, null);

            var promo = await _db.PromoCodes.FirstOrDefaultAsync(p => p.Code == couponCode);
            if (promo == null) return (false, "Invalid coupon code.");
            if (!promo.IsActive) return (false, "Coupon is not active.");
            if (promo.ExpiresAt.HasValue && promo.ExpiresAt.Value < DateTime.UtcNow)
                return (false, "Coupon has expired.");
            if (promo.MaxRedemptions.HasValue && promo.RedemptionCount >= promo.MaxRedemptions.Value)
                return (false, "Coupon redemption limit reached.");

            promo.RedemptionCount += 1;
            await _db.SaveChangesAsync();

            return (true, $"Coupon '{promo.Code}' applied successfully.");
        }


        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        private static string Sha256Hex(string input)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private async Task<(string plainToken, EmailVerificationToken record)>
        CreateEmailVerifyTokenAsync(User user, TimeSpan ttl)
        {
            var raw = RandomNumberGenerator.GetBytes(32);
            var plain = Base64Url(raw);
            var hash = Sha256Hex(plain);

            var rec = new EmailVerificationToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = hash,
                ExpiresAt = DateTime.UtcNow.Add(ttl),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.EmailVerificationTokens.Add(rec);
            await _db.SaveChangesAsync();
            return (plain, rec);
        }


        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto, string? origin = null)
        {
            var email = NormalizeEmail(dto.Email);
            await EnsureEmailAvailableAsync(email);

            var studentRole = await _db.Roles.FirstAsync(r => r.Name == "Student");

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = studentRole.Id,
                IsActive = true,
                DateCreated = DateTime.UtcNow,

                Phone = dto.Phone ?? string.Empty,
                Gender = dto.Gender,
                Country = dto.Country ?? string.Empty
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var couponResult = await TryApplyCouponAsync(dto.CouponCode);

            var (plainToken, _) = await CreateEmailVerifyTokenAsync(user, TimeSpan.FromDays(3));

            var brand = SiteBrandResolver.Resolve(origin, _config);
            var apiBase = _config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5245";
            var verifyUrl = $"{apiBase}/api/auth/verify-email?token={Uri.EscapeDataString(plainToken)}&site={brand.SiteKey}";
            var loginUrl = $"{brand.FrontendBaseUrl}/login";

            var subject = $"🎉 Welcome to {brand.AppName} – The Future of Learning Awaits!";
            var html = EmailTemplates.BuildWelcomeEmailHtml(
                appName: brand.AppName,
                role: "Student",
                firstName: FirstNameOf(user.FullName),
                verifyLink: verifyUrl,
                supportEmail: brand.SupportEmail,
                supportPhone: brand.SupportPhone
            );

            try
            {
                await _email.SendAsync(user.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
            }
            catch (Exception ex)
            {

                _logger.LogWarning(ex, "Welcome email failed for {Email}; registration still succeeded", user.Email);
            }

            var res = await GenerateAuthResponse(user);
            res.PromoApplied = couponResult.applied;
            res.PromoMessage = couponResult.message;
            return res;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

            bool passwordValid;
            try   { passwordValid = user != null && BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash); }
            catch { passwordValid = false; }

            if (user == null || !passwordValid)
                throw new Exception("Invalid credentials");

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> LoginForRoleAsync(LoginDto dto, string requiredRole, int tokenExpiryHours)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            var actualRole = user.Role?.Name
                ?? await _db.Roles.Where(r => r.Id == user.RoleId).Select(r => r.Name).FirstOrDefaultAsync()
                ?? string.Empty;

            if (!actualRole.Equals(requiredRole, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"{requiredRole} access required");

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedAccessException("Refresh token is required.");

            var handler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal;
            try
            {
                principal = handler.ValidateToken(
                    refreshToken,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = _config["Jwt:Issuer"],
                        ValidAudience = _config["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetRefreshSecret())),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    },
                    out _);
            }
            catch
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            var tokenType = principal.FindFirst("token_type")?.Value;
            if (!string.Equals(tokenType, "refresh", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Invalid refresh token.");

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst("sub")?.Value;
            if (!Guid.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid refresh token.");

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                throw new UnauthorizedAccessException("User not found or inactive.");

            var roleName = user.Role?.Name ?? await GetRoleNameAsync(user.RoleId);
            var ttl = AccessTokenTtlForRole(roleName);
            return await GenerateAuthResponse(user, ttl);
        }

        public async Task<AuthResponseDto> AdminCreateUserAsync(AdminCreateUserDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Email already exists");

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = dto.RoleId,
                SchoolId = dto.SchoolId,
                IsActive = true,
                DateCreated = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Create email verification token & send welcome/verification email (admin-created users should still verify)
            try
            {
                var (plainToken, _) = await CreateEmailVerifyTokenAsync(user, TimeSpan.FromDays(3));

                var brand = SiteBrandResolver.Resolve(null, _config);
                var apiBase = _config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5245";
                var verifyUrl = $"{apiBase}/api/auth/verify-email?token={Uri.EscapeDataString(plainToken)}&site={brand.SiteKey}";
                var loginUrl = $"{brand.FrontendBaseUrl}/login";

                var roleName = await GetRoleNameAsync(user.RoleId);
                var subject = $"🎉 Welcome to {brand.AppName} – The Future of Learning Awaits!";
                var html = EmailTemplates.BuildWelcomeEmailHtml(
                    appName: brand.AppName,
                    role: roleName,
                    firstName: FirstNameOf(user.FullName),
                    verifyLink: verifyUrl,
                    supportEmail: brand.SupportEmail,
                    supportPhone: brand.SupportPhone
                );

                await _email.SendAsync(user.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Welcome email failed for admin-created user {Email}; user creation still succeeded", user.Email);
            }

            var couponResult = await TryApplyCouponAsync(dto.CouponCode);
            var res = await GenerateAuthResponse(user);
            res.PromoApplied = couponResult.applied;
            res.PromoMessage = couponResult.message;
            return res;
        }

        public async Task<AuthResponseDto> RegisterSchoolAsync(RegisterSchoolDto dto, string? origin = null)
        {
            var email = NormalizeEmail(dto.Email);
            await EnsureEmailAvailableAsync(email);

            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var existingPromo = await _db.PromoCodes.FirstOrDefaultAsync(p => p.Code == dto.CouponCode);
                if (existingPromo == null || !existingPromo.IsActive || (existingPromo.ExpiresAt.HasValue && existingPromo.ExpiresAt.Value < DateTime.UtcNow))
                    throw new Exception("Invalid or expired coupon code.");
            }

            var schoolAdminRole = await _db.Roles.FirstAsync(r => r.Name == "SchoolAdmin");

            var sub = string.IsNullOrWhiteSpace(dto.Subdomain) ? Slugify(dto.SchoolName) : Slugify(dto.Subdomain);
            var baseSub = sub; int i = 1;
            while (await _db.Schools.AnyAsync(s => s.Subdomain == sub))
                sub = $"{baseSub}-{i++}";

            var school = new School
            {
                Id = Guid.NewGuid(),
                Name = dto.SchoolName,
                Subdomain = sub,
                IsActive = true,
                DateCreated = DateTime.UtcNow,
                Country = dto.Country,
                TotalStudents = dto.TotalStudents,
                ContactName = dto.FullName,
                ContactEmail = email,
                ContactPhone = dto.Phone,
                ContactPosition = dto.Position,
                EstimatedScienceStudentCount = dto.EstimatedScienceStudentCount,
                DisabledStudentCount = dto.DisabledStudentCount
            };
            _db.Schools.Add(school);

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = schoolAdminRole.Id,
                SchoolId = school.Id,
                IsActive = true,
                DateCreated = DateTime.UtcNow,

                Phone = dto.Phone,
                Country = dto.Country,
            };
            _db.Users.Add(user);


            var trialDays     = _config.GetValue<int>("Subscriptions:TrialDays", 14);
            var trialStudents = _config.GetValue<int>("Subscriptions:TrialStudentCount", 30);
            var now           = DateTime.UtcNow;
            _db.Subscriptions.Add(new Subscription
            {
                SchoolId             = school.Id,
                UserId               = null,
                StudentsCovered      = trialStudents,
                PricePerStudent      = 0m,
                StartsAt             = now,
                EndsAt               = now.AddDays(trialDays),
                Active               = true,
                LastPaymentReference = "TRIAL"
            });


            var (plainToken, _) = await CreateEmailVerifyTokenAsync(user, TimeSpan.FromDays(3));

            var brand = SiteBrandResolver.Resolve(origin, _config);
            var apiBase = _config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5245";
            var verifyUrl = $"{apiBase}/api/auth/verify-email?token={Uri.EscapeDataString(plainToken)}&site={brand.SiteKey}";
            var loginUrl = $"{brand.FrontendBaseUrl}/login";

            var subject = $"🎉 Welcome to {brand.AppName} – The Future of Learning Awaits!";
            var html = EmailTemplates.BuildWelcomeEmailHtml(
                appName: brand.AppName,
                role: schoolAdminRole.Name,
                firstName: FirstNameOf(user.FullName),
                verifyLink: verifyUrl,
                supportEmail: brand.SupportEmail,
                supportPhone: brand.SupportPhone
            );

            try
            {
                await _email.SendAsync(user.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
            }
            catch (Exception ex)
            {

                _logger.LogWarning(ex, "Welcome email failed for school admin {Email}; registration still succeeded", user.Email);
            }

            await _db.SaveChangesAsync();


            var couponResult = await TryApplyCouponAsync(dto.CouponCode);

            var res = await GenerateAuthResponse(user);
            res.PromoApplied = couponResult.applied;
            res.PromoMessage = couponResult.message;
            return res;
        }

        public async Task VerifyEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new Exception("Invalid token.");

            var tokenHash = Sha256Hex(token);

            var rec = await _db.EmailVerificationTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == tokenHash);

            if (rec is null)
                throw new Exception("Invalid verification link.");
            if (rec.IsUsed)
                return;
            if (rec.ExpiresAt < DateTime.UtcNow)
                throw new Exception("This link has expired. Please request a new one.");

            rec.IsUsed = true;


            rec.User.IsEmailVerified = true;
            rec.User.EmailVerifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }



public async Task RequestPasswordResetAsync(string email, string? origin = null)
{
    email = email?.Trim()?.ToLowerInvariant() ?? "";
    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email is required");

    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    if (user == null)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(100, 300)));
        return;
    }

    var raw = RandomNumberGenerator.GetBytes(32);
    var plainToken = Base64Url(raw);
    var tokenHash = Sha256Hex(plainToken);

    var resetToken = new PasswordResetToken
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        TokenHash = tokenHash,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        IsUsed = false
    };

    _db.PasswordResetTokens.Add(resetToken);
    await _db.SaveChangesAsync();

    var brand = SiteBrandResolver.Resolve(origin, _config);
    var resetUrl = $"{brand.FrontendBaseUrl}/auth/reset-password?token={Uri.EscapeDataString(plainToken)}";

    var subject = $"🔐 Reset Your {brand.AppName} Password";
    var html = EmailTemplates.BuildPasswordResetEmailHtml(
        FirstNameOf(user.FullName), resetUrl, brand.SupportEmail, brand.SupportPhone, brand.AppName);

    await _email.SendAsync(user.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
}

public async Task ResetPasswordAsync(string token, string newPassword, string? origin = null)
{
    if (string.IsNullOrWhiteSpace(token))
        throw new ArgumentException("Token is required");

    if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        throw new ArgumentException("Password must be at least 8 characters");

    var tokenHash = Sha256Hex(token);

    var resetToken = await _db.PasswordResetTokens
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    if (resetToken == null)
        throw new Exception("Invalid or expired reset link");
    if (resetToken.IsUsed)
        throw new Exception("This reset link has already been used");
    if (resetToken.ExpiresAt < DateTime.UtcNow)
        throw new Exception("This reset link has expired. Please request a new one.");

    resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    resetToken.IsUsed = true;
    await _db.SaveChangesAsync();

    var brand = SiteBrandResolver.Resolve(origin, _config);
    var loginUrl = $"{brand.FrontendBaseUrl}/login";

    var subject = "✅ Your Password Has Been Changed";
    var html = EmailTemplates.BuildPasswordChangedEmailHtml(
        FirstNameOf(resetToken.User.FullName), loginUrl, brand.SupportEmail, brand.SupportPhone, brand.AppName);

    try
    {
        await _email.SendAsync(resetToken.User.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Password reset confirmation email failed for {Email}; password reset still succeeded", resetToken.User.Email);
    }
}

public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string? origin = null)
{
    if (string.IsNullOrWhiteSpace(currentPassword))
        throw new ArgumentException("Current password is required");

    if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        throw new ArgumentException("New password must be at least 8 characters");

    if (currentPassword == newPassword)
        throw new ArgumentException("New password must be different from current password");

    var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
        throw new Exception("User not found");

    if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        throw new Exception("Current password is incorrect");

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    await _db.SaveChangesAsync();

    var brand = SiteBrandResolver.Resolve(origin, _config);
    var loginUrl = $"{brand.FrontendBaseUrl}/login";

    var subject = "✅ Your Password Has Been Changed";
    var html = EmailTemplates.BuildPasswordChangedEmailHtml(
        FirstNameOf(user.FullName), loginUrl, brand.SupportEmail, brand.SupportPhone, brand.AppName);

    try
    {
        await _email.SendAsync(user.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Password change confirmation email failed for {Email}; password change still succeeded", user.Email);
    }
}
        public async Task ResendVerificationAsync(string email, string? origin = null)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found");

            var (plainToken, _) = await CreateEmailVerifyTokenAsync(user, TimeSpan.FromDays(3));

            var brand = SiteBrandResolver.Resolve(origin, _config);
            var apiBase = _config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5245";
            var verifyUrl = $"{apiBase}/api/auth/verify-email?token={Uri.EscapeDataString(plainToken)}&site={brand.SiteKey}";
            var loginUrl = $"{brand.FrontendBaseUrl}/login";

            var roleName = await GetRoleNameAsync(user.RoleId);
            var subject = $"🔄 Verify your {brand.AppName} account";

            var html = EmailTemplates.BuildWelcomeEmailHtml(
                appName: brand.AppName,
                role: roleName,
                firstName: FirstNameOf(user.FullName),
                verifyLink: verifyUrl,
                supportEmail: brand.SupportEmail,
                supportPhone: brand.SupportPhone
            );

            try
            {
                await _email.SendAsync(user.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Verification email resend failed for {Email}; verification token was still created", user.Email);
            }
        }



        private record GoogleTokenInfo(string Email, string Name, string GoogleSub, string? Picture);

        private async Task<GoogleTokenInfo> ValidateGoogleTokenAsync(string idToken, CancellationToken ct)
        {
            if (_config.GetValue<bool>("Testing:AllowFakeGoogleSignIn") &&
                idToken == "e2e-google-token")
            {
                return new GoogleTokenInfo(
                    Email: "testuser@bluesandstemlabs.com",
                    Name: "Test Individual User",
                    GoogleSub: "e2e-google-subject",
                    Picture: null);
            }

            if (string.IsNullOrWhiteSpace(idToken))
                throw new UnauthorizedAccessException("Invalid or expired Google token.");

            var clientId = _config["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("Google:ClientId is not configured.");

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                if (!payload.EmailVerified)
                    throw new UnauthorizedAccessException("Invalid or expired Google token.");

                var email = NormalizeEmail(payload.Email);
                if (string.IsNullOrWhiteSpace(email))
                    throw new UnauthorizedAccessException("Invalid or expired Google token.");

                return new GoogleTokenInfo(
                    Email: email,
                    Name: payload.Name ?? string.Empty,
                    GoogleSub: payload.Subject ?? string.Empty,
                    Picture: payload.Picture);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google token validation failed");
                throw new UnauthorizedAccessException("Invalid or expired Google token.");
            }
        }


        public async Task<AuthResponseDto> GoogleSignInAsync(string idToken, CancellationToken ct = default)
        {
            var tokenInfo = await ValidateGoogleTokenAsync(idToken, ct);

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == tokenInfo.Email, ct);

            if (user == null)
                return await CreateGoogleUserAsync(tokenInfo, phone: null, country: null, couponCode: null, origin: null, ct);

            if (string.IsNullOrWhiteSpace(user.GoogleSubject))
                throw new UserAlreadyExistsException("An account with this email already exists. Please login with your password.");

            if (!string.Equals(user.GoogleSubject, tokenInfo.GoogleSub, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Invalid or expired Google token.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("User not found or inactive.");


            if (!user.IsEmailVerified)
            {
                user.IsEmailVerified = true;
                user.EmailVerifiedAt = DateTime.UtcNow;
            }

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return await GenerateAuthResponse(user);
        }


        public async Task<AuthResponseDto> GoogleSignUpAsync(GoogleSignUpDto dto, string? origin = null, CancellationToken ct = default)
        {
            var tokenInfo = await ValidateGoogleTokenAsync(dto.IdToken, ct);

            var existing = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == tokenInfo.Email, ct);

            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.GoogleSubject))
                    throw new UserAlreadyExistsException("An account with this email already exists. Please login with your password.");

                if (!string.Equals(existing.GoogleSubject, tokenInfo.GoogleSub, StringComparison.Ordinal))
                    throw new UnauthorizedAccessException("Invalid or expired Google token.");

                if (!existing.IsActive)
                    throw new UnauthorizedAccessException("User not found or inactive.");

                if (!existing.IsEmailVerified)
                {
                    existing.IsEmailVerified = true;
                    existing.EmailVerifiedAt = DateTime.UtcNow;
                }
                existing.LastLogin = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return await GenerateAuthResponse(existing);
            }

            return await CreateGoogleUserAsync(tokenInfo, dto.Phone, dto.Country, dto.CouponCode, origin, ct);
        }

        private async Task<AuthResponseDto> CreateGoogleUserAsync(
            GoogleTokenInfo tokenInfo,
            string? phone,
            string? country,
            string? couponCode,
            string? origin,
            CancellationToken ct)
        {
            var studentRole = await _db.Roles.FirstAsync(r => r.Name == "Student", ct);

            var user = new User
            {
                Id              = Guid.NewGuid(),
                FullName        = string.IsNullOrWhiteSpace(tokenInfo.Name) ? tokenInfo.Email : tokenInfo.Name,
                Email           = tokenInfo.Email,

                PasswordHash    = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                RoleId          = studentRole.Id,
                IsActive        = true,
                DateCreated     = DateTime.UtcNow,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                Phone           = phone ?? string.Empty,
                Country         = country ?? string.Empty,
                GoogleSubject   = tokenInfo.GoogleSub
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);


            try
            {
                var brand    = SiteBrandResolver.Resolve(origin, _config);
                var loginUrl = $"{brand.FrontendBaseUrl}/login";
                var subject  = $"🎉 Welcome to {brand.AppName} – The Future of Learning Awaits!";
                var html     = EmailTemplates.BuildWelcomeEmailHtml(
                    appName:      brand.AppName,
                    role:         "Student",
                    firstName:    FirstNameOf(user.FullName),
                    verifyLink:   loginUrl,
                    supportEmail: brand.SupportEmail,
                    supportPhone: brand.SupportPhone
                );
                await _email.SendAsync(user.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Welcome email failed for Google user {Email}; sign-up still succeeded", user.Email);
            }

            var couponResult = await TryApplyCouponAsync(couponCode);
            var res          = await GenerateAuthResponse(user);
            res.PromoApplied = couponResult.applied;
            res.PromoMessage = couponResult.message;
            return res;
        }


private async Task<AuthResponseDto> GenerateAuthResponse(User user, TimeSpan? tokenTtl = null)
{
    var secret = GetJwtSecret();
    var issuer = _config["Jwt:Issuer"] ?? string.Empty;
    var audience = _config["Jwt:Audience"] ?? string.Empty;
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

    var roleName = user.Role?.Name
        ?? await _db.Roles.Where(r => r.Id == user.RoleId)
                           .Select(r => r.Name)
                           .FirstOrDefaultAsync()
        ?? string.Empty;

    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim("FullName", user.FullName ?? string.Empty),
        new Claim("role", roleName)
    };

    if (user.SchoolId.HasValue && user.SchoolId.Value != Guid.Empty)
        claims.Add(new Claim("SchoolId", user.SchoolId.Value.ToString()));

    var minutes = _config.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 120;
    var accessTtl = tokenTtl ?? TimeSpan.FromMinutes(minutes);
    var accessExpiresAt = DateTime.UtcNow.Add(accessTtl);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Issuer = issuer,
        Audience = audience,
        Expires = accessExpiresAt,
        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    };

    var handler = new JwtSecurityTokenHandler();
    var securityToken = handler.CreateToken(tokenDescriptor);
    var tokenString = handler.WriteToken(securityToken);
    var refreshDays = _config.GetValue<int?>("Jwt:RefreshTokenDays") ?? 14;
    var refreshExpiresAt = DateTime.UtcNow.AddDays(refreshDays);
    var refreshToken = BuildRefreshToken(user, roleName, refreshExpiresAt);


    string? schoolName = null;
    string schoolCurrency = "NGN";
    if (user.SchoolId.HasValue && user.SchoolId.Value != Guid.Empty)
    {
        var school = await _db.Schools
            .Where(s => s.Id == user.SchoolId.Value)
            .Select(s => new { s.Name, s.Currency })
            .FirstOrDefaultAsync();
        schoolName = school?.Name;
        schoolCurrency = school?.Currency ?? "NGN";
    }

    var response = new AuthResponseDto
    {
        Token = tokenString,
        AccessTokenExpiresAt = accessExpiresAt,
        RefreshToken = refreshToken,
        RefreshTokenExpiresAt = refreshExpiresAt,
        FullName = user.FullName ?? string.Empty,
        Role = roleName,
        UserId = user.Id,
        SchoolId = user.SchoolId,
        SchoolName = schoolName,
        SchoolCurrency = schoolCurrency,
        Email = user.Email ?? string.Empty,
        IsVerified = user.IsEmailVerified,
        Phone = user.Phone ?? string.Empty,
        Country = user.Country ?? string.Empty
    };


    var schoolId = user.SchoolId ?? Guid.Empty;
    
    Subscription? subscription = null;
    
    if (schoolId != Guid.Empty)
    {

        subscription = await _db.Subscriptions
            .Where(s => s.SchoolId == schoolId && s.Active)
            .OrderByDescending(s => s.EndsAt)
            .FirstOrDefaultAsync();
    }
    else
    {

        subscription = await _db.Subscriptions
            .Where(s => s.UserId == user.Id && s.Active)
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

        var tier = await _db.PricingTiers
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

    return response;
}
    }
}
