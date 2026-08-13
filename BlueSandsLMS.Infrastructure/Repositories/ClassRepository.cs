using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;

public class ClassRepository : IClassRepository
{
    private readonly BlueSandsLMSDbContext _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<ClassRepository> _logger;

    public ClassRepository(
        BlueSandsLMSDbContext db,
        IEmailService email,
        IConfiguration config,
        ILogger<ClassRepository> logger)
    {
        _db = db;
        _email = email;
        _config = config;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(Guid schoolId, Guid teacherId, string name, string subject)
    {
        var cls = new Classroom
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Subject = subject,
            CreatedAt = DateTime.UtcNow
        };
        _db.Classrooms.Add(cls);

        _db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(),
            ClassroomId = cls.Id,
            UserId = teacherId,
            RoleInClass = ClassRole.Teacher,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return cls.Id;
    }

    public async Task UpdateAsync(Guid classId, string name, string subject)
    {
        var cls = await _db.Classrooms.FindAsync(classId) ?? throw new Exception("Class not found");
        cls.Name = name;
        cls.Subject = subject;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid classId)
    {
        var cls = await _db.Classrooms.FindAsync(classId) ?? throw new Exception("Class not found");
        _db.Classrooms.Remove(cls);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UserIsTeacherAsync(Guid classId, Guid userId)
    {

        if (await _db.Enrollments.AnyAsync(e =>
                e.ClassroomId == classId &&
                e.UserId == userId &&
                e.RoleInClass == ClassRole.Teacher))
            return true;


        if (await _db.ClassroomTeachers.AnyAsync(ct =>
                ct.ClassroomId == classId && ct.TeacherUserId == userId))
            return true;


        var classroom = await _db.Classrooms
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new { c.SchoolId })
            .FirstOrDefaultAsync();

        if (classroom == null) return false;

        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.SchoolId == classroom.SchoolId)
            .Select(u => new { RoleName = u.Role!.Name })
            .FirstOrDefaultAsync();

        return user?.RoleName == "SchoolAdmin";
    }

    public async Task EnrollByEmailAsync(Guid classId, string email)
    {
        email = email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {

            var classroom = await _db.Classrooms.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == classId)
                ?? throw new InvalidOperationException("Classroom not found");

            var studentRoleId = await _db.Roles
                .Where(r => r.Name == "Student")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (studentRoleId == Guid.Empty)
                throw new InvalidOperationException("Student role not configured");

            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FullName = email.Split('@')[0],

                PasswordHash = "INVITE_PENDING",
                RoleId = studentRoleId,
                SchoolId = classroom.SchoolId,
                IsActive = true,
                IsEmailVerified = false,
                DateCreated = DateTime.UtcNow
            };
            _db.Users.Add(user);
        }

        var exists = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId && e.UserId == user.Id);
        if (!exists)
        {
            _db.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                ClassroomId = classId,
                UserId = user.Id,
                RoleInClass = ClassRole.Student,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task BulkEnrollAsync(Guid classId, IEnumerable<string> emails)
    {
        var emailList = emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (emailList.Count == 0) return;

        var users = await _db.Users
            .Where(u => emailList.Contains(u.Email))
            .Select(u => u.Id)
            .ToListAsync();

        var existing = await _db.Enrollments
            .Where(e => e.ClassroomId == classId)
            .Select(e => e.UserId)
            .ToListAsync();

        var toAdd = users.Except(existing).ToList();
        foreach (var uid in toAdd)
        {
            _db.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                ClassroomId = classId,
                UserId = uid,
                RoleInClass = ClassRole.Student,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
    }


    public async Task<(string code, DateTime? expiresAt)> RotateInviteCodeAsync(Guid classId, int expireDays)
    {
        var c = await _db.Classrooms.FindAsync(classId) ?? throw new Exception("Class not found");
        (c.InviteCode, c.InviteCodeExpiresAt) = GenerateInvite(expireDays <= 0 ? 14 : expireDays);
        await _db.SaveChangesAsync();
        return (c.InviteCode!, c.InviteCodeExpiresAt);
    }

    public async Task<Guid?> GetClassroomIdByInviteAsync(string code)
    {
        code = code.Trim();
        return await _db.Classrooms
            .Where(c => c.InviteCode == code && (c.InviteCodeExpiresAt == null || c.InviteCodeExpiresAt >= DateTime.UtcNow))
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task JoinByCodeAsync(Guid userId, string code)
    {
        var classId = await GetClassroomIdByInviteAsync(code) ?? throw new Exception("Invalid or expired code.");
        var exists = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId && e.UserId == userId);
        if (exists) return;

        _db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(),
            ClassroomId = classId,
            UserId = userId,
            RoleInClass = ClassRole.Student,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<ClassSummaryDto>> GetMyClassesAsync(Guid userId)
    {
        var mine = await (from e in _db.Enrollments
                          join c in _db.Classrooms on e.ClassroomId equals c.Id
                          where e.UserId == userId
                          select new
                          {
                              c.Id,
                              c.Name,
                              c.Subject,
                              e.RoleInClass,
                              c.CreatedAt,
                              c.InviteCode,
                              c.InviteCodeExpiresAt
                          }).ToListAsync();

        var ids = mine.Select(x => x.Id).ToList();

        var studentCounts = await _db.Enrollments
            .Where(en => ids.Contains(en.ClassroomId) && en.RoleInClass == ClassRole.Student)
            .GroupBy(en => en.ClassroomId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToListAsync();

        return mine.Select(x => new ClassSummaryDto(
            x.Id, x.Name, x.Subject,
            x.RoleInClass == ClassRole.Teacher ? ClassRoleDto.Teacher : ClassRoleDto.Student,
            studentCounts.FirstOrDefault(s => s.ClassId == x.Id)?.Count ?? 0,
            x.CreatedAt, x.InviteCode, x.InviteCodeExpiresAt
        )).ToList();
    }

    public async Task<List<ClassSummaryDto>> GetClassesBySchoolIdAsync(Guid schoolId)
    {

        var list = await _db.Classrooms
            .Where(c => c.SchoolId == schoolId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Subject,
                c.CreatedAt,
                c.InviteCode,
                c.InviteCodeExpiresAt
            })
            .ToListAsync();

        var ids = list.Select(x => x.Id).ToList();
        var studentCounts = await _db.Enrollments
            .Where(e => ids.Contains(e.ClassroomId) && e.RoleInClass == ClassRole.Student)
            .GroupBy(e => e.ClassroomId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToListAsync();


        return list.Select(x => new ClassSummaryDto(
            x.Id,
            x.Name,
            x.Subject,
            ClassRoleDto.Teacher,
            studentCounts.FirstOrDefault(s => s.ClassId == x.Id)?.Count ?? 0,
            x.CreatedAt,
            x.InviteCode,
            x.InviteCodeExpiresAt
        )).ToList();
    }

    public async Task AttachTeacherAsync(Guid classId, Guid teacherUserId)
    {
        var classroom = await _db.Classrooms.FindAsync(classId) ?? throw new InvalidOperationException("Class not found");
        var teacher = await _db.Users.FindAsync(teacherUserId) ?? throw new InvalidOperationException("User not found");

        if (teacher.SchoolId != classroom.SchoolId)
            throw new InvalidOperationException("Teacher does not belong to this school");

        var alreadyTeacherViaEnrollment = await _db.Enrollments.AnyAsync(e =>
            e.ClassroomId == classId && e.UserId == teacherUserId && e.RoleInClass == ClassRole.Teacher);

        var alreadyTeacherViaAssignment = await _db.ClassroomTeachers.AnyAsync(ct =>
            ct.ClassroomId == classId && ct.TeacherUserId == teacherUserId);

        if (alreadyTeacherViaEnrollment || alreadyTeacherViaAssignment)
            return;

        _db.ClassroomTeachers.Add(new ClassroomTeacher
        {
            ClassroomId = classId,
            TeacherUserId = teacherUserId,
            AssignedAt = DateTime.UtcNow
        });

        // If the teacher user appears to be an invited placeholder (INVITE_PENDING) or has no password,
        // generate a password and send credentials via email.
        var shouldCreateCredentials = string.IsNullOrWhiteSpace(teacher.PasswordHash) ||
                                      string.Equals(teacher.PasswordHash, "INVITE_PENDING", StringComparison.Ordinal);

        string? plainPassword = null;

        if (shouldCreateCredentials)
        {
            plainPassword = GenerateSecurePassword(12);
            teacher.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            teacher.IsActive = true;

            // mark email verified = false by default; don't flip verification here
            // (admins may still want explicit verify flow). Do not auto-verify.
        }

        await _db.SaveChangesAsync();

        // Always attempt to notify the teacher about the class assignment.
        if (!string.IsNullOrWhiteSpace(teacher.Email))
        {
            try
            {
                var brand = SiteBrandResolver.Resolve(null, _config);
                var loginUrl = $"{brand.FrontendBaseUrl}/login";
                var classUrl = $"{brand.FrontendBaseUrl}/class/{classId}";
                var subject = $"You were assigned as a teacher to \"{classroom.Name}\" on {brand.AppName}";
                var firstName = FirstNameOf(teacher.FullName);

                var sb = new StringBuilder();
                sb.Append(@"<!doctype html><html><body style=""font-family:Arial,Helvetica,sans-serif;color:#111;line-height:1.6"">");
                sb.AppendFormat("<p>Dear {0},</p>", WebUtility.HtmlEncode(firstName));
                sb.AppendFormat("<p>You have been assigned as a <strong>Teacher</strong> for the class <strong>{0}</strong> on <strong>{1}</strong>.</p>",
                    WebUtility.HtmlEncode(classroom.Name), WebUtility.HtmlEncode(brand.AppName));

                sb.AppendFormat("<p>Open the class: <a href=\"{0}\">{1}</a></p>", WebUtility.HtmlEncode(classUrl), WebUtility.HtmlEncode(classUrl));

                if (!string.IsNullOrWhiteSpace(plainPassword))
                {
                    sb.Append(@"<p style=""padding:12px;background:#f3f4f6;border-radius:6px;""><strong>Your credentials</strong><br/>");
                    sb.AppendFormat("Username: {0}<br/>", WebUtility.HtmlEncode(teacher.Email));
                    sb.AppendFormat("Password: <strong>{0}</strong></p>", WebUtility.HtmlEncode(plainPassword));
                    sb.AppendFormat("<p>Please sign in at <a href=\"{0}\">{1}</a> and change your password immediately.</p>", WebUtility.HtmlEncode(loginUrl), WebUtility.HtmlEncode(loginUrl));
                }
                else
                {
                    sb.AppendFormat("<p>If you don't already have an account, sign in at <a href=\"{0}\">{1}</a> to access the class. If you already have an account, just sign in and you'll see the class listed.</p>", WebUtility.HtmlEncode(loginUrl), WebUtility.HtmlEncode(loginUrl));
                }

                sb.AppendFormat("<p>If you have any trouble, contact support at <a href=\"mailto:{0}\">{0}</a>.</p>", WebUtility.HtmlEncode(brand.SupportEmail));
                sb.AppendFormat("<p>Kind regards,<br/>{0} Team</p>", WebUtility.HtmlEncode(brand.AppName));
                sb.Append("</body></html>");

                var html = sb.ToString();

                await _email.SendAsync(teacher.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send teacher assignment email to {Email} for class {ClassId}", teacher.Email, classId);
            }
        }
    }

    public async Task AttachStudentAsync(Guid classId, Guid studentUserId)
    {
        var classroom = await _db.Classrooms.FindAsync(classId) ?? throw new InvalidOperationException("Class not found");
        var student = await _db.Users.FindAsync(studentUserId) ?? throw new InvalidOperationException("User not found");

        if (student.SchoolId != classroom.SchoolId)
            throw new InvalidOperationException("Student does not belong to this school");

        // If already enrolled in the class (any role), do nothing.
        var alreadyEnrolled = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId && e.UserId == studentUserId);
        if (alreadyEnrolled) return;

        _db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(),
            ClassroomId = classId,
            UserId = studentUserId,
            RoleInClass = ClassRole.Student,
            CreatedAt = DateTime.UtcNow
        });

        // If the student user appears to be an invited placeholder (INVITE_PENDING) or has no password,
        // generate a password and make the account active.
        var shouldCreateCredentials = string.IsNullOrWhiteSpace(student.PasswordHash) ||
                                      string.Equals(student.PasswordHash, "INVITE_PENDING", StringComparison.Ordinal);

        string? plainPassword = null;

        if (shouldCreateCredentials)
        {
            plainPassword = GenerateSecurePassword(10);
            student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            student.IsActive = true;
            // Keep IsEmailVerified = false; verify flow should remain explicit.
        }

        await _db.SaveChangesAsync();

        // Notify the student by email about their class assignment (include credentials if created)
        if (!string.IsNullOrWhiteSpace(student.Email))
        {
            try
            {
                var brand = SiteBrandResolver.Resolve(null, _config);
                var loginUrl = $"{brand.FrontendBaseUrl}/login";
                var classUrl = $"{brand.FrontendBaseUrl}/class/{classId}";
                var subject = $"You were added to the class \"{classroom.Name}\" on {brand.AppName}";
                var firstName = FirstNameOf(student.FullName);

                var sb = new StringBuilder();
                sb.Append(@"<!doctype html><html><body style=""font-family:Arial,Helvetica,sans-serif;color:#111;line-height:1.6"">");
                sb.AppendFormat("<p>Dear {0},</p>", WebUtility.HtmlEncode(firstName));
                sb.AppendFormat("<p>You have been added as a <strong>Student</strong> to the class <strong>{0}</strong> on <strong>{1}</strong>.</p>",
                    WebUtility.HtmlEncode(classroom.Name), WebUtility.HtmlEncode(brand.AppName));

                sb.AppendFormat("<p>Open the class: <a href=\"{0}\">{1}</a></p>", WebUtility.HtmlEncode(classUrl), WebUtility.HtmlEncode(classUrl));

                if (!string.IsNullOrWhiteSpace(plainPassword))
                {
                    sb.Append(@"<p style=""padding:12px;background:#f3f4f6;border-radius:6px;""><strong>Your credentials</strong><br/>");
                    sb.AppendFormat("Username: {0}<br/>", WebUtility.HtmlEncode(student.Email));
                    sb.AppendFormat("Password: <strong>{0}</strong></p>", WebUtility.HtmlEncode(plainPassword));
                    sb.AppendFormat("<p>Please sign in at <a href=\"{0}\">{1}</a> and change your password after first login.</p>", WebUtility.HtmlEncode(loginUrl), WebUtility.HtmlEncode(loginUrl));
                }
                else
                {
                    sb.AppendFormat("<p>If you don't already have an account, sign in at <a href=\"{0}\">{1}</a> to access the class. If you already have an account, simply sign in and the class will be listed.</p>", WebUtility.HtmlEncode(loginUrl), WebUtility.HtmlEncode(loginUrl));
                }

                sb.AppendFormat("<p>If you have any trouble, contact support at <a href=\"mailto:{0}\">{0}</a>.</p>", WebUtility.HtmlEncode(brand.SupportEmail));
                sb.AppendFormat("<p>Kind regards,<br/>{0} Team</p>", WebUtility.HtmlEncode(brand.AppName));
                sb.Append("</body></html>");

                var html = sb.ToString();

                await _email.SendAsync(student.Email, subject, html, brand.FromEmail, brand.FromDisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send student assignment email to {Email} for class {ClassId}", student.Email, classId);
            }
        }
    }


    private static (string code, DateTime? expiresAt) GenerateInvite(int expireDays)
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var buf = new byte[5]; rng.GetBytes(buf);
        var code = Convert.ToBase64String(buf).Replace("+", "A").Replace("/", "B").Replace("=", "").ToUpper();
        return (code, DateTime.UtcNow.AddDays(expireDays));
    }

    private static string GenerateSecurePassword(int length)
    {
        const string lowers = "abcdefghijklmnopqrstuvwxyz";
        const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*()_-+=[]{}|;:,.<>?";

        var all = lowers + uppers + digits + symbols;
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);

        // Ensure at least one of each required char type for stronger passwords
        sb.Append(lowers[bytes[0] % lowers.Length]);
        sb.Append(uppers[bytes[1] % uppers.Length]);
        sb.Append(digits[bytes[2] % digits.Length]);
        sb.Append(symbols[bytes[3] % symbols.Length]);

        for (int i = 4; i < length; i++)
        {
            sb.Append(all[bytes[i] % all.Length]);
        }

        // Simple shuffle
        var arr = sb.ToString().ToCharArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            var tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }

        return new string(arr);
    }

    private static string FirstNameOf(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "there";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "there";
    }

    public static class SiteBrandResolver
    {
        public static SiteBrand Resolve(Guid? schoolId, IConfiguration config)
        {
            // TODO: swap for real per-tenant branding lookup if one exists
            return new SiteBrand(
                AppName: config["Branding:AppName"] ?? "BlueSandsLMS",
                FrontendBaseUrl: config["Branding:FrontendBaseUrl"] ?? "https://app.bluesandslms.com",
                SupportEmail: config["Branding:SupportEmail"] ?? "support@bluesandslms.com",
                FromEmail: config["Branding:FromEmail"] ?? "no-reply@bluesandslms.com",
                FromDisplayName: config["Branding:FromDisplayName"] ?? "BlueSandsLMS"
            );
        }
    }

    public record SiteBrand(
        string AppName,
        string FrontendBaseUrl,
        string SupportEmail,
        string FromEmail,
        string FromDisplayName
    );
}