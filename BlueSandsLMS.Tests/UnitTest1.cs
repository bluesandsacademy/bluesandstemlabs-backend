using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using BlueSandsLMS.Api.Controllers;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BlueSandsLMS.Tests
{
    public class RepositoryTests
    {
        private BlueSandsLMSDbContext CreateContext(string name = null)
        {
            var options = new DbContextOptionsBuilder<BlueSandsLMSDbContext>()
                .UseInMemoryDatabase(databaseName: name ?? Guid.NewGuid().ToString())
                .Options;
            return new BlueSandsLMSDbContext(options);
        }

        [Fact]
        public async Task GetClassesBySchoolIdAsync_ReturnsOnlySchoolMatches()
        {
            using var db = CreateContext();
            var repo = new ClassRepository(db);

            var schoolA = Guid.NewGuid();
            var schoolB = Guid.NewGuid();
            var teacher = Guid.NewGuid();

            db.Classrooms.Add(new Classroom { Id = Guid.NewGuid(), SchoolId = schoolA, Name = "A1", Subject = "X", CreatedAt = DateTime.UtcNow });
            db.Classrooms.Add(new Classroom { Id = Guid.NewGuid(), SchoolId = schoolA, Name = "A2", Subject = "Y", CreatedAt = DateTime.UtcNow });
            db.Classrooms.Add(new Classroom { Id = Guid.NewGuid(), SchoolId = schoolB, Name = "B1", Subject = "Z", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var list = await repo.GetClassesBySchoolIdAsync(schoolA);
            Assert.Equal(2, list.Count);
            Assert.All(list, c => Assert.Equal(schoolA, db.Classrooms.First(x => x.Id == c.Id).SchoolId));
        }

        [Fact]
        public async Task ClassesEndpoint_SchoolIdQueryParam_RespectsRoles()
        {
            using var db = CreateContext();
            var repo = new ClassRepository(db);
            var service = repo as IClassRepository;

            var callerSchool = Guid.NewGuid();
            var otherSchool = Guid.NewGuid();

            db.Classrooms.Add(new Classroom { Id = Guid.NewGuid(), SchoolId = callerSchool, Name = "Mine", Subject = "S" , CreatedAt=DateTime.UtcNow});
            db.Classrooms.Add(new Classroom { Id = Guid.NewGuid(), SchoolId = otherSchool, Name = "Other", Subject = "S" , CreatedAt=DateTime.UtcNow});
            await db.SaveChangesAsync();

            var ctrl = new ClassesController(service);

            void SetUser(Guid userId, string role, Guid? schoolId)
            {
                var claims = new[] {
                    new Claim("sub", userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                };
                if (schoolId.HasValue)
                    claims = claims.Append(new Claim("SchoolId", schoolId.Value.ToString())).ToArray();
                ctrl.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
                };
            }

            SetUser(Guid.NewGuid(), "Teacher", callerSchool);
            var forb = await ctrl.BySchool(otherSchool) as ForbidResult;
            Assert.NotNull(forb);

            var ok1 = await ctrl.BySchool(callerSchool) as OkObjectResult;
            Assert.NotNull(ok1);
            var list1 = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<ClassSummaryDto>>(ok1.Value);
            Assert.Single(list1);

            SetUser(Guid.NewGuid(), "SchoolAdmin", callerSchool);
            var ok2 = await ctrl.BySchool(otherSchool) as OkObjectResult;
            Assert.NotNull(ok2);
            var list2 = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<ClassSummaryDto>>(ok2.Value);
            Assert.Single(list2);
        }
    }

    public class IlsControllerTests
    {
        private BlueSandsLMSDbContext CreateContext(string? name = null)
        {
            var options = new DbContextOptionsBuilder<BlueSandsLMSDbContext>()
                .UseInMemoryDatabase(databaseName: name ?? Guid.NewGuid().ToString())
                .Options;
            return new BlueSandsLMSDbContext(options);
        }

        private IlsController CreateController(BlueSandsLMSDbContext db, Guid userId, string role = "Teacher")
        {
            var ctrl = new IlsController(db, NullLogger<IlsController>.Instance);
            var claims = new[] {
                new Claim("sub", userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "test");
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return ctrl;
        }

        private static CreateIlsRequest BuildCreateIlsRequest(Guid simulationId, Guid tagId)
        {
            return new CreateIlsRequest
            {
                Title = "Pendulum Lab",
                Objective = "Explore pendulum motion",
                Duration = "0.25",
                SimulationId = simulationId.ToString(),
                Tags = new() { tagId.ToString() },
                PreSimAssessment = new QuizData
                {
                    QuizTitle = "Pre-lab",
                    Questions = new()
                    {
                        new QuizQuestion
                        {
                            Question = "What do you expect?",
                            Options = new() { "Fast", "Slow" }
                        }
                    }
                },
                PostSimAssessment = new QuizData
                {
                    QuizTitle = "Post-lab",
                    Points = "2",
                    Questions = new()
                    {
                        new QuizQuestion
                        {
                            Question = "What changed?"
                        }
                    }
                }
            };
        }

        private static string BuildAssessmentConfigJson()
        {
            return JsonSerializer.Serialize(new
            {
                score = "Grade 8",
                duration = "0.75",
                introductionMessage = "Welcome to the lab",
                engagementQuestion = "What do you notice?",
                hypothesisQuestion = "What do you predict?",
                experimentProcedures = new[] { "Measure", "Record" },
                discussionPrompt = "Discuss the results",
                realWorldApplications = new[] { "Engineering" },
                relatedCareers = new[] { "Scientist" },
                realWorldTask = "Design an experiment",
                preSimAssessment = new QuizData
                {
                    QuizTitle = "Pre-lab",
                    Description = "Before the simulation",
                    Points = "1",
                    Questions = new()
                    {
                        new QuizQuestion
                        {
                            Question = "What do you expect?",
                            Options = new() { "Fast", "Slow" },
                            CorrectAnswer = "Fast"
                        }
                    }
                },
                postSimAssessment = new QuizData
                {
                    QuizTitle = "Post-lab",
                    Description = "After the simulation",
                    Points = "2",
                    Questions = new()
                    {
                        new QuizQuestion
                        {
                            Question = "What changed?",
                            Options = new() { "Only one" },
                            CorrectAnswer = "Only one"
                        }
                    }
                }
            });
        }

        [Fact]
        public async Task List_NoClassId_ReturnsOnlyOwned()
        {
            using var db = CreateContext();
            var me = Guid.NewGuid();
            var other = Guid.NewGuid();

            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace { Id = Guid.NewGuid(), CreatedBy = me, Status = IlsStatus.Draft, DurationMinutes = 15 });
            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace { Id = Guid.NewGuid(), CreatedBy = other, Status = IlsStatus.Draft, DurationMinutes = 30 });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, me);
            var ok = await ctrl.List(null, CancellationToken.None) as OkObjectResult;
            Assert.NotNull(ok);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<IlsDetailsDto>>(ok.Value);
            var arr = list.ToArray();
            Assert.Single(arr);
            Assert.Equal(me, arr[0].CreatedBy);
            Assert.Equal("0.25", arr[0].Duration);
        }

        [Fact]
        public async Task List_WithClassId_ReturnsPublishedForClass()
        {
            using var db = CreateContext();
            var teacher = Guid.NewGuid();
            var student = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var ils1 = Guid.NewGuid();
            var ils2 = Guid.NewGuid();

            db.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassroomId = classId, UserId = teacher, RoleInClass = ClassRole.Teacher, CreatedAt = DateTime.UtcNow });

            db.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassroomId = classId, UserId = student, RoleInClass = ClassRole.Student, CreatedAt = DateTime.UtcNow });

            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace { Id = ils1, CreatedBy = teacher, Status = IlsStatus.Published, DurationMinutes = 30 });
            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace { Id = ils2, CreatedBy = teacher, Status = IlsStatus.Draft, DurationMinutes = 10 });
            db.StudentIlsSessions.Add(new StudentIlsSession { Id = Guid.NewGuid(), StudentId = student, IlsId = ils1 });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, teacher);
            var ok = await ctrl.List(classId, CancellationToken.None) as OkObjectResult;
            Assert.NotNull(ok);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<IlsDetailsDto>>(ok.Value);
            var arr = list.ToArray();
            Assert.Single(arr);
            Assert.Equal(ils1, arr[0].Id);
            Assert.Equal("0.5", arr[0].Duration);
        }

        [Fact]
        public async Task Student_CanListAndGetAssignedPublishedIls()
        {
            using var db = CreateContext();
            var teacher = Guid.NewGuid();
            var student = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var ilsId = Guid.NewGuid();
            var simulationId = Guid.NewGuid();
            var tagId = Guid.NewGuid();

            db.Classrooms.Add(new Classroom
            {
                Id = classId,
                SchoolId = Guid.NewGuid(),
                Name = "JSS 2A",
                Subject = "Physics",
                CreatedAt = DateTime.UtcNow
            });
            db.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassroomId = classId, UserId = teacher, RoleInClass = ClassRole.Teacher, CreatedAt = DateTime.UtcNow });
            db.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassroomId = classId, UserId = student, RoleInClass = ClassRole.Student, CreatedAt = DateTime.UtcNow });
            db.CurriculumTags.Add(new CurriculumTag
            {
                Id = tagId,
                Label = "Motion",
                Subject = "Physics",
                CreatedAt = DateTime.UtcNow
            });
            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace
            {
                Id = ilsId,
                Title = "Density Lab",
                Objective = "Measure density",
                Grade = "Grade 8",
                DurationMinutes = 45,
                SimulationId = simulationId,
                AssessmentConfigJson = BuildAssessmentConfigJson(),
                PollOptionsJson = JsonSerializer.Serialize(new[] { "Fast", "Slow" }),
                Status = IlsStatus.Published,
                CreatedBy = teacher,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.IlsTags.Add(new IlsTag
            {
                IlsId = ilsId,
                TagId = tagId
            });
            db.Assignments.Add(new Assignment
            {
                Id = Guid.NewGuid(),
                ClassroomId = classId,
                Title = "Density Lab",
                Type = BlueSandsLMS.Core.Entities.AssignmentType.Lesson,
                ResourceCode = $"ils:{ilsId:D}",
                CreatedByUserId = teacher,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, student, "Student");

            var listOk = await ctrl.List(classId, CancellationToken.None) as OkObjectResult;
            Assert.NotNull(listOk);
            var list = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<IlsDetailsDto>>(listOk.Value);
            Assert.Single(list);
            Assert.Equal(ilsId, list.Single().Id);
            Assert.Equal("0.75", list.Single().Duration);

            var getOk = await ctrl.GetById(ilsId, CancellationToken.None) as OkObjectResult;
            Assert.NotNull(getOk);
            var dto = Assert.IsType<IlsFormDataDto>(getOk.Value);
            Assert.Equal(ilsId, dto.Id);
            Assert.Equal("Grade 8", dto.Score);
            Assert.Equal("0.75", dto.Duration);
            Assert.Equal(simulationId.ToString(), dto.SimulationId);
            Assert.Equal(new[] { "Motion" }, dto.Tags);
            Assert.Equal("Pre-lab", dto.PreSimAssessment.QuizTitle);
            Assert.Equal("Post-lab", dto.PostSimAssessment.QuizTitle);
            Assert.Equal("Welcome to the lab", dto.IntroductionMessage);
            Assert.Equal("What do you notice?", dto.EngagementQuestion);
            Assert.Equal("What do you predict?", dto.HypothesisQuestion);
            Assert.Equal(new[] { "Measure", "Record" }, dto.ExperimentProcedures);
            Assert.Equal("Discuss the results", dto.DiscussionPrompt);
            Assert.Equal(new[] { "Engineering" }, dto.RealWorldApplications);
            Assert.Equal(new[] { "Scientist" }, dto.RelatedCareers);
            Assert.Equal("Design an experiment", dto.RealWorldTask);
            Assert.Equal("published", dto.Status);
        }

        [Fact]
        public async Task Create_AllowsDraftWithoutScore()
        {
            using var db = CreateContext();
            var teacher = Guid.NewGuid();
            var simulationId = Guid.NewGuid();
            var tagId = Guid.NewGuid();

            db.PhETSimulations.Add(new PhETSimulation
            {
                Id = simulationId,
                Title = "Pendulum",
                IsActive = true
            });
            db.CurriculumTags.Add(new CurriculumTag
            {
                Id = tagId,
                Label = "Motion",
                Subject = "Physics",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, teacher);
            var request = BuildCreateIlsRequest(simulationId, tagId);
            request.Score = string.Empty;

            var result = await ctrl.Create(request, CancellationToken.None) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);

            var saved = await db.InteractiveLearningSpaces.SingleAsync();
            Assert.Equal(15, saved.DurationMinutes);
            Assert.Equal(string.Empty, saved.Grade);
            Assert.Equal(IlsStatus.Draft, saved.Status);
        }

        [Fact]
        public async Task Update_AllowsPartialUpdateWithoutScore()
        {
            using var db = CreateContext();
            var teacher = Guid.NewGuid();
            var simulationId = Guid.NewGuid();
            var ilsId = Guid.NewGuid();

            db.PhETSimulations.Add(new PhETSimulation
            {
                Id = simulationId,
                Title = "Pendulum",
                IsActive = true
            });
            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace
            {
                Id = ilsId,
                Title = "Original Title",
                Objective = "Original Objective",
                Grade = "Grade 8",
                DurationMinutes = 30,
                SimulationId = simulationId,
                Status = IlsStatus.Draft,
                CreatedBy = teacher,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, teacher);
            var result = await ctrl.Update(ilsId, new UpdateIlsRequest
            {
                Title = "Updated Title"
            }, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);

            var saved = await db.InteractiveLearningSpaces.SingleAsync();
            Assert.Equal("Updated Title", saved.Title);
            Assert.Equal("Grade 8", saved.Grade);
        }

        [Fact]
        public async Task Update_RejectsBlankScore()
        {
            using var db = CreateContext();
            var teacher = Guid.NewGuid();
            var simulationId = Guid.NewGuid();
            var ilsId = Guid.NewGuid();

            db.PhETSimulations.Add(new PhETSimulation
            {
                Id = simulationId,
                Title = "Pendulum",
                IsActive = true
            });
            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace
            {
                Id = ilsId,
                Title = "Original Title",
                Objective = "Original Objective",
                Grade = "Grade 8",
                DurationMinutes = 30,
                SimulationId = simulationId,
                Status = IlsStatus.Draft,
                CreatedBy = teacher,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, teacher);
            var result = await ctrl.Update(ilsId, new UpdateIlsRequest
            {
                Score = "   "
            }, CancellationToken.None) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task Assign_CreatesAssignmentForPublishedIls()
        {
            using var db = CreateContext();
            var teacher = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var ilsId = Guid.NewGuid();

            db.Classrooms.Add(new Classroom
            {
                Id = classId,
                SchoolId = Guid.NewGuid(),
                Name = "JSS 3B",
                Subject = "Chemistry",
                CreatedAt = DateTime.UtcNow
            });
            db.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                ClassroomId = classId,
                UserId = teacher,
                RoleInClass = ClassRole.Teacher,
                CreatedAt = DateTime.UtcNow
            });
            db.InteractiveLearningSpaces.Add(new InteractiveLearningSpace
            {
                Id = ilsId,
                Title = "Acids and Bases",
                Objective = "Classify materials",
                Grade = "Grade 9",
                DurationMinutes = 25,
                SimulationId = Guid.NewGuid(),
                Status = IlsStatus.Published,
                CreatedBy = teacher,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, teacher);
            var result = await ctrl.Assign(ilsId, new AssignIlsRequest
            {
                ClassroomId = classId,
                Type = "classwork"
            }, CancellationToken.None) as OkObjectResult;

            Assert.NotNull(result);

            var assignment = await db.Assignments.SingleAsync();
            Assert.Equal(classId, assignment.ClassroomId);
            Assert.Equal("Acids and Bases", assignment.Title);
            Assert.Equal($"ils:{ilsId:D}", assignment.ResourceCode);
            Assert.Equal(BlueSandsLMS.Core.Entities.AssignmentType.Lesson, assignment.Type);
            Assert.Equal(teacher, assignment.CreatedByUserId);
        }

        [Fact]
        public async Task Create_AutoCreatesNewTags_WhenTagLabelsDontExist()
        {
            using var db = CreateContext();
            var teacher = Guid.NewGuid();
            var simulationId = Guid.NewGuid();

            db.PhETSimulations.Add(new PhETSimulation
            {
                Id = simulationId,
                Title = "Pendulum",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db, teacher);
            var request = new CreateIlsRequest
            {
                Title = "Test ILS",
                Objective = "Test Objective",
                Duration = "0.5",
                SimulationId = simulationId.ToString(),
                Tags = new() { "NewTag1", "NewTag2" },
                PreSimAssessment = new QuizData
                {
                    QuizTitle = "Pre-lab",
                    Questions = new()
                    {
                        new QuizQuestion
                        {
                            Question = "What do you expect?",
                            Options = new() { "Fast", "Slow" }
                        }
                    }
                },
                PostSimAssessment = new QuizData
                {
                    QuizTitle = "Post-lab",
                    Points = "2",
                    Questions = new()
                    {
                        new QuizQuestion
                        {
                            Question = "What changed?"
                        }
                    }
                }
            };

            var result = await ctrl.Create(request, CancellationToken.None) as ObjectResult;
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);

            var saved = await db.InteractiveLearningSpaces.SingleAsync();
            Assert.Equal(30, saved.DurationMinutes);
            Assert.Equal(IlsStatus.Draft, saved.Status);

            var createdTags = await db.CurriculumTags.ToListAsync();
            Assert.Equal(2, createdTags.Count);

            var ilsTags = await db.IlsTags.Where(it => it.IlsId == saved.Id).ToListAsync();
            Assert.Equal(2, ilsTags.Count);
        }
    }
}
