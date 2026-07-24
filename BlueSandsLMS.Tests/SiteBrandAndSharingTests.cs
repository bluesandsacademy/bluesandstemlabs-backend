using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BlueSandsLMS.Application.Emails;
using BlueSandsLMS.Api.Controllers;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BlueSandsLMS.Tests
{

    public class EmailTemplatesBrandingTests
    {
        [Fact]
        public void BuildWelcomeEmailHtml_DefaultAppName_ContainsBlueSands()
        {
            var html = EmailTemplates.BuildWelcomeEmailHtml(
                role: "Student", firstName: "Ada",
                loginLink: "http://login", verifyLink: "http://verify",
                supportEmail: "support@bluesandstemlabs.com", supportPhone: "+234");

            Assert.Contains("Blue Sands STEM Labs", html);
        }
    }

    public class IlsSharingTests
    {
        private static BlueSandsLMSDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<BlueSandsLMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static IlsController CreateController(BlueSandsLMSDbContext db, Guid userId,
            Guid? schoolId = null, string role = "Teacher")
        {
            var ctrl = new IlsController(db, NullLogger<IlsController>.Instance);
            var claims = new List<Claim>
            {
                new("sub", userId.ToString()),
                new(ClaimTypes.Role, role)
            };
            if (schoolId.HasValue)
                claims.Add(new Claim("SchoolId", schoolId.Value.ToString()));

            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            };
            return ctrl;
        }

        private static async Task<(Guid userId, Guid schoolId, Guid ilsId)> SeedSharedIlsAsync(
            BlueSandsLMSDbContext db, bool isShared = true, bool isPublished = true)
        {
            var owner = Guid.NewGuid();
            var schoolId = Guid.NewGuid();
            var ilsId = Guid.NewGuid();

            db.Users.Add(new User
            {
                Id = owner, FullName = "Owner", Email = $"{owner}@test.com",
                PasswordHash = "x", SchoolId = schoolId, IsActive = true,
                DateCreated = DateTime.UtcNow
            });

            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace
            {
                Id = ilsId, Title = "Shared ILS", Objective = "Test",
                Grade = "Grade 8", DurationMinutes = 30,
                SimulationId = Guid.NewGuid(),
                Status = isPublished ? IlsStatus.Published : IlsStatus.Draft,
                CreatedBy = owner,
                SchoolId = schoolId,
                IsSharedWithSchool = isShared,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return (owner, schoolId, ilsId);
        }

        [Fact]
        public async Task List_SameSchoolTeacher_SeesSharedIls()
        {
            using var db = CreateContext();
            var (_, schoolId, ilsId) = await SeedSharedIlsAsync(db);

            var colleague = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = colleague, FullName = "Colleague", Email = $"{colleague}@test.com",
                PasswordHash = "x", SchoolId = schoolId, IsActive = true,
                DateCreated = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, colleague, schoolId);
            var ok = await ctrl.List(null, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(ok);
            var list = Assert.IsAssignableFrom<IEnumerable<IlsDetailsDto>>(ok.Value).ToArray();
            Assert.Single(list);
            Assert.Equal(ilsId, list[0].Id);
            Assert.True(list[0].IsSharedWithSchool);
        }

        [Fact]
        public async Task List_DifferentSchoolTeacher_DoesNotSeeSharedIls()
        {
            using var db = CreateContext();
            await SeedSharedIlsAsync(db);

            var otherSchool = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = stranger, FullName = "Stranger", Email = $"{stranger}@test.com",
                PasswordHash = "x", SchoolId = otherSchool, IsActive = true,
                DateCreated = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, stranger, otherSchool);
            var ok = await ctrl.List(null, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(ok);
            var list = Assert.IsAssignableFrom<IEnumerable<IlsDetailsDto>>(ok.Value).ToArray();
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetById_ColleagueCanReadSharedIls()
        {
            using var db = CreateContext();
            var (_, schoolId, ilsId) = await SeedSharedIlsAsync(db);

            var colleague = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = colleague, FullName = "Colleague", Email = $"{colleague}@test.com",
                PasswordHash = "x", SchoolId = schoolId, IsActive = true,
                DateCreated = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, colleague, schoolId);
            var result = await ctrl.GetById(ilsId, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetById_StrangerCannotReadSharedIls()
        {
            using var db = CreateContext();
            var (_, _, ilsId) = await SeedSharedIlsAsync(db);

            var stranger = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = stranger, FullName = "Stranger", Email = $"{stranger}@test.com",
                PasswordHash = "x", SchoolId = Guid.NewGuid(), IsActive = true,
                DateCreated = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, stranger, Guid.NewGuid());
            var result = await ctrl.GetById(ilsId, CancellationToken.None) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task Share_OwnerCanShareIls()
        {
            using var db = CreateContext();
            var (owner, schoolId, ilsId) = await SeedSharedIlsAsync(db, isShared: false);

            var ctrl = CreateController(db, owner, schoolId);
            var result = await ctrl.Share(ilsId, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            var saved = await db.InteractiveLearningSpaces.SingleAsync();
            Assert.True(saved.IsSharedWithSchool);
        }

        [Fact]
        public async Task Unshare_OwnerCanUnshareIls()
        {
            using var db = CreateContext();
            var (owner, schoolId, ilsId) = await SeedSharedIlsAsync(db, isShared: true);

            var ctrl = CreateController(db, owner, schoolId);
            var result = await ctrl.Unshare(ilsId, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            var saved = await db.InteractiveLearningSpaces.SingleAsync();
            Assert.False(saved.IsSharedWithSchool);
        }

        [Fact]
        public async Task Share_NonOwnerCannotShareIls()
        {
            using var db = CreateContext();
            var (_, schoolId, ilsId) = await SeedSharedIlsAsync(db, isShared: false);

            var colleague = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = colleague, FullName = "Colleague", Email = $"{colleague}@test.com",
                PasswordHash = "x", SchoolId = schoolId, IsActive = true,
                DateCreated = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, colleague, schoolId);
            var result = await ctrl.Share(ilsId, CancellationToken.None) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task Assign_ColleagueCanAssignSharedPublishedIls()
        {
            using var db = CreateContext();
            var (_, schoolId, ilsId) = await SeedSharedIlsAsync(db, isShared: true, isPublished: true);

            var colleague = Guid.NewGuid();
            var classId = Guid.NewGuid();

            db.Users.Add(new User
            {
                Id = colleague, FullName = "Colleague", Email = $"{colleague}@test.com",
                PasswordHash = "x", SchoolId = schoolId, IsActive = true,
                DateCreated = DateTime.UtcNow
            });
            db.Classrooms.Add(new Classroom
            {
                Id = classId, SchoolId = schoolId, Name = "JSS 2A",
                Subject = "Physics", CreatedAt = DateTime.UtcNow
            });
            db.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(), ClassroomId = classId, UserId = colleague,
                RoleInClass = ClassRole.Teacher, CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, colleague, schoolId);
            var result = await ctrl.Assign(ilsId, new AssignIlsRequest
            {
                ClassroomId = classId, Type = "classwork"
            }, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);
            Assert.True(await db.Assignments.AnyAsync());
        }
    }
}
