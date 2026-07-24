using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Infrastructure.Repositories
{
    public class ClassRepository : IClassRepository
    {
        private readonly BlueSandsLMSDbContext _db;
        public ClassRepository(BlueSandsLMSDbContext db) => _db = db;

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
                    Id       = Guid.NewGuid(),
                    Email    = email,
                    FullName = email.Split('@')[0],

                    PasswordHash    = "INVITE_PENDING",
                    RoleId          = studentRoleId,
                    SchoolId        = classroom.SchoolId,
                    IsActive        = true,
                    IsEmailVerified = false,
                    DateCreated     = DateTime.UtcNow
                };
                _db.Users.Add(user);
            }

            var exists = await _db.Enrollments.AnyAsync(e => e.ClassroomId == classId && e.UserId == user.Id);
            if (!exists)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    Id          = Guid.NewGuid(),
                    ClassroomId = classId,
                    UserId      = user.Id,
                    RoleInClass = ClassRole.Student,
                    CreatedAt   = DateTime.UtcNow
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
                                  c.Id, c.Name, c.Subject, e.RoleInClass, c.CreatedAt, c.InviteCode, c.InviteCodeExpiresAt
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


        private static (string code, DateTime? expiresAt) GenerateInvite(int expireDays)
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var buf = new byte[5]; rng.GetBytes(buf);
            var code = Convert.ToBase64String(buf).Replace("+", "A").Replace("/", "B").Replace("=", "").ToUpper();
            return (code, DateTime.UtcNow.AddDays(expireDays));
        }
    }
}
