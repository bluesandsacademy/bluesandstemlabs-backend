using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BlueSandsLMS.Application.Emails;

namespace BlueSandsLMS.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly IConfiguration _config;
        private readonly IEmailService _email;

        public AuthService(BlueSandsLMSDbContext db, IConfiguration config, IEmailService email)
        {
            _db = db;
            _config = config;
            _email = email;
        }

        // -----------------------
        // Helpers
        // -----------------------
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

        // -----------------------
        // Email verification helpers (secure)
        // -----------------------
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
            var plain = Base64Url(raw);        // goes in the link
            var hash = Sha256Hex(plain);       // saved to DB

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

        // -----------------------
        // Public methods
        // -----------------------
        public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Email already exists");

            var studentRole = await _db.Roles.FirstAsync(r => r.Name == "Student");

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = studentRole.Id,
                IsActive = true,
                DateCreated = DateTime.UtcNow,

                Phone = dto.Phone ?? string.Empty,
                Country = dto.Country ?? string.Empty
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Apply coupon exactly once and surface message
            var couponResult = await TryApplyCouponAsync(dto.CouponCode);

            // Create verification token + send welcome email
            var (plainToken, _) = await CreateEmailVerifyTokenAsync(user, TimeSpan.FromDays(3));

            var apiBase = _config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5245";
            var feBase = _config["App:FrontendBaseUrl"]?.TrimEnd('/') ?? apiBase;

            var verifyUrl = $"{apiBase}/api/auth/verify-email?token={Uri.EscapeDataString(plainToken)}";
            var loginUrl = $"{feBase}/login";

            var subject = "🎉 Welcome to Blue Sands STEM Labs – The Future of Learning Awaits!";
            var html = EmailTemplates.BuildWelcomeEmailHtml(
                role: "Student",
                firstName: FirstNameOf(user.FullName),
                loginLink: loginUrl,
                verifyLink: verifyUrl,
                supportEmail: _config["App:SupportEmail"] ?? "support@bluesandstemlabs.com",
                supportPhone: _config["App:SupportPhone"] ?? "+234 7034194669"
            );

            await _email.SendAsync(user.Email, subject, html);

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

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await GenerateAuthResponse(user);
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

            var couponResult = await TryApplyCouponAsync(dto.CouponCode);
            var res = await GenerateAuthResponse(user);
            res.PromoApplied = couponResult.applied;
            res.PromoMessage = couponResult.message;
            return res;
        }

        public async Task<AuthResponseDto> RegisterSchoolAsync(RegisterSchoolDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("Email already exists");

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
                ContactEmail = dto.Email,
                ContactPhone = dto.Phone,
                ContactPosition = dto.Position
            };
            _db.Schools.Add(school);

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = schoolAdminRole.Id,
                SchoolId = school.Id,
                IsActive = true,
                DateCreated = DateTime.UtcNow,

                Phone = dto.Phone,
                Country = dto.Country,
            };
            _db.Users.Add(user);

            // Create verification token + send welcome email (SchoolAdmin)
            var (plainToken, _) = await CreateEmailVerifyTokenAsync(user, TimeSpan.FromDays(3));

            var apiBase = _config["App:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5245";
            var feBase = _config["App:FrontendBaseUrl"]?.TrimEnd('/') ?? apiBase;

            var verifyUrl = $"{apiBase}/api/auth/verify-email?token={Uri.EscapeDataString(plainToken)}";
            var loginUrl = $"{feBase}/login";

            var subject = "🎉 Welcome to Blue Sands STEM Labs – The Future of Learning Awaits!";
            var html = EmailTemplates.BuildWelcomeEmailHtml(
                role: schoolAdminRole.Name, // <-- fixes 'role not found' error
                firstName: FirstNameOf(user.FullName),
                loginLink: loginUrl,
                verifyLink: verifyUrl,
                supportEmail: _config["App:SupportEmail"] ?? "support@bluesandstemlabs.com",
                supportPhone: _config["App:SupportPhone"] ?? "+234 7034194669"
            );

            await _email.SendAsync(user.Email, subject, html);

            await _db.SaveChangesAsync();

            // Apply coupon exactly once and surface message
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
                return; // idempotent: already verified
            if (rec.ExpiresAt < DateTime.UtcNow)
                throw new Exception("This link has expired. Please request a new one.");

            rec.IsUsed = true;

            // ✅ actually mark user as verified
            rec.User.IsEmailVerified = true;
            rec.User.EmailVerifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task ResendVerificationAsync(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                throw new Exception("User not found");

            // If user already verified, bail out
            // (uncomment if you have IsEmailVerified field)
            // if (user.IsEmailVerified) 
            //     throw new Exception("Email already verified");

            // Generate a new token (reuse secure token logic)
            var (plainToken, _) = await CreateEmailVerifyTokenAsync(user, TimeSpan.FromDays(3));

            var apiBase = _config["App:BaseUrl"]?.TrimEnd('/');
            var feBase = _config["App:FrontendBaseUrl"]?.TrimEnd('/');

            var verifyUrl = $"{apiBase}/api/auth/verify-email?token={Uri.EscapeDataString(plainToken)}";
            var loginUrl = $"{feBase}/login";

            var roleName = await GetRoleNameAsync(user.RoleId);
            var subject = "🔄 Verify your Blue Sands STEM Labs account";

            var html = EmailTemplates.BuildWelcomeEmailHtml(
                roleName,
                FirstNameOf(user.FullName),
                loginUrl,
                verifyUrl,
                _config["App:SupportEmail"] ?? "support@bluesandstemlabs.com",
                _config["App:SupportPhone"] ?? "+234 7034194669"
            );

            await _email.SendAsync(user.Email, subject, html);
        }

        // -----------------------
        // JWT builder
        // -----------------------
        private async Task<AuthResponseDto> GenerateAuthResponse(User user)
        {
            var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
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
        new Claim(ClaimTypes.Role, roleName)
    };

            if (user.SchoolId.HasValue && user.SchoolId.Value != Guid.Empty)
                claims.Add(new Claim("SchoolId", user.SchoolId.Value.ToString()));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = issuer,
                Audience = audience,
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(tokenDescriptor);
            var tokenString = handler.WriteToken(securityToken);

            var response = new AuthResponseDto
            {
                Token = tokenString,
                FullName = user.FullName ?? string.Empty,
                Role = roleName,
                UserId = user.Id,
                SchoolId = user.SchoolId,
                Email = user.Email ?? string.Empty,
                IsVerified = user.IsEmailVerified,
                Phone = user.Phone ?? string.Empty,
                Country = user.Country ?? string.Empty
            };

            // 🔹 Enrich with subscription + tier
            if (user.SchoolId.HasValue && user.SchoolId.Value != Guid.Empty)
            {
                var schoolId = user.SchoolId.Value;

                var sub = await _db.Subscriptions
                    .Where(s => s.SchoolId == schoolId)
                    .OrderByDescending(s => s.Active)
                    .ThenByDescending(s => s.EndsAt)
                    .FirstOrDefaultAsync();

                if (sub != null)
                {
                    // sub.EndsAt is non-nullable DateTime in your entity → no HasValue/Value
                    var end = sub.EndsAt;
                    var daysRemaining = Math.Max(0, (int)Math.Floor((end - DateTime.UtcNow).TotalDays));

                    response.Subscription = new SubscriptionSummaryDto
                    {
                        Active = sub.Active,
                        StartsAt = sub.StartsAt,
                        EndsAt = sub.EndsAt, // assigning DateTime to DateTime? is fine
                        StudentsCovered = sub.StudentsCovered,
                        PricePerStudent = sub.PricePerStudent,
                        LastPaymentReference = sub.LastPaymentReference,
                        DaysRemaining = daysRemaining
                    };

                    // Match tier by students covered
                    var students = sub.StudentsCovered;

                    var tier = await _db.PricingTiers
                        .OrderBy(t => t.MinStudents)
                        .FirstOrDefaultAsync(t => students >= t.MinStudents && students <= t.MaxStudents);

                    if (tier != null)
                    {
                        response.CurrentTier = new TierSummaryDto
                        {
                            Id = tier.Id,                 // int
                            TierName = tier.TierName,
                            MinStudents = tier.MinStudents,
                            MaxStudents = tier.MaxStudents,
                            PricePerStudent = tier.PricePerStudent,
                            IsMatch = true
                        };
                    }
                }
            }

            return response;
        }
    }
}