using System.Text.Json;
using BlueSandsLMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlueSandsLMS.Infrastructure.Setup
{
    public static class UatSeedDataService
    {
        private const string SharedPasswordHash = "$2a$11$cEhjobe.nmtMXJHMXQWhW.a7HJrFSOdBhXdqYi2Oj4BYeTh9LXC2y";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static async Task SeedAsync(
            BlueSandsLMSDbContext db,
            ILogger logger,
            CancellationToken ct = default)
        {
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                var now = DateTime.UtcNow;
                await using var tx = await db.Database.BeginTransactionAsync(ct);

                var teacherRoleId = await ResolveRoleIdAsync(
                    db,
                    "Teacher",
                    Guid.Parse("d7c51101-d2a4-40d5-bb0a-bd97898cf847"),
                    ct);
                var studentRoleId = await ResolveRoleIdAsync(
                    db,
                    "Student",
                    Guid.Parse("ae17f104-0ec3-47e3-9517-0e7e2c3be8b0"),
                    ct);

                var teacherOneId = await EnsureUserAsync(
                    db,
                    email: "uat.teacher1@bluesands.local",
                    fullName: "UAT Teacher One",
                    roleId: teacherRoleId,
                    fallbackId: Guid.Parse("71000000-0000-0000-0000-000000000001"),
                    createdAt: now.AddDays(-14),
                    ct);

                var teacherTwoId = await EnsureUserAsync(
                    db,
                    email: "uat.teacher2@bluesands.local",
                    fullName: "UAT Teacher Two",
                    roleId: teacherRoleId,
                    fallbackId: Guid.Parse("71000000-0000-0000-0000-000000000002"),
                    createdAt: now.AddDays(-14),
                    ct);

                var studentIds = new List<Guid>
                {
                    await EnsureUserAsync(db, "uat.student1@bluesands.local", "UAT Student One", studentRoleId, Guid.Parse("72000000-0000-0000-0000-000000000001"), now.AddDays(-14), ct),
                    await EnsureUserAsync(db, "uat.student2@bluesands.local", "UAT Student Two", studentRoleId, Guid.Parse("72000000-0000-0000-0000-000000000002"), now.AddDays(-14), ct),
                    await EnsureUserAsync(db, "uat.student3@bluesands.local", "UAT Student Three", studentRoleId, Guid.Parse("72000000-0000-0000-0000-000000000003"), now.AddDays(-14), ct),
                    await EnsureUserAsync(db, "uat.student4@bluesands.local", "UAT Student Four", studentRoleId, Guid.Parse("72000000-0000-0000-0000-000000000004"), now.AddDays(-14), ct),
                    await EnsureUserAsync(db, "uat.student5@bluesands.local", "UAT Student Five", studentRoleId, Guid.Parse("72000000-0000-0000-0000-000000000005"), now.AddDays(-14), ct)
                };

                var chemistrySimId = await EnsureSimulationAsync(
                    db,
                    title: "UAT Chemistry Density Lab",
                    fallbackId: Guid.Parse("73000000-0000-0000-0000-000000000001"),
                    subject: "chemistry",
                    previewUrl: "https://cdn.bluesands.local/simulations/chem-density/preview.png",
                    runUrl: "https://cdn.bluesands.local/simulations/chem-density/index.html",
                    config: new { sliders = new[] { "density", "volume", "temp" }, chart = "line", subject = "chemistry", gradeBand = "7-9" },
                    ct);

                var biologySimId = await EnsureSimulationAsync(
                    db,
                    title: "UAT Biology Cell Diffusion Lab",
                    fallbackId: Guid.Parse("73000000-0000-0000-0000-000000000002"),
                    subject: "biology",
                    previewUrl: "https://cdn.bluesands.local/simulations/bio-diffusion/preview.png",
                    runUrl: "https://cdn.bluesands.local/simulations/bio-diffusion/index.html",
                    config: new { sliders = new[] { "concentration", "membranePermeability" }, chart = "scatter", subject = "biology", gradeBand = "8-10" },
                    ct);

                var physicsSimId = await EnsureSimulationAsync(
                    db,
                    title: "UAT Physics Force Motion Lab",
                    fallbackId: Guid.Parse("73000000-0000-0000-0000-000000000003"),
                    subject: "physics",
                    previewUrl: "https://cdn.bluesands.local/simulations/physics-motion/preview.png",
                    runUrl: "https://cdn.bluesands.local/simulations/physics-motion/index.html",
                    config: new { sliders = new[] { "mass", "force", "friction" }, chart = "bar", subject = "physics", gradeBand = "8-11" },
                    ct);

                var tagSeeds = new (string Label, string Subject, Guid FallbackId)[]
                {
                    ("Matter and Density - Grade 8", "Chemistry", Guid.Parse("74000000-0000-0000-0000-000000000001")),
                    ("States of Matter - Grade 7", "Chemistry", Guid.Parse("74000000-0000-0000-0000-000000000002")),
                    ("Thermal Energy - Grade 8", "Physics", Guid.Parse("74000000-0000-0000-0000-000000000003")),
                    ("Molecular Motion - Grade 9", "Chemistry", Guid.Parse("74000000-0000-0000-0000-000000000004")),
                    ("Data Interpretation - Grade 8", "Cross-Disciplinary", Guid.Parse("74000000-0000-0000-0000-000000000005")),
                    ("Force and Motion - Grade 9", "Physics", Guid.Parse("74000000-0000-0000-0000-000000000006")),
                    ("Newtonian Mechanics - Grade 10", "Physics", Guid.Parse("74000000-0000-0000-0000-000000000007")),
                    ("Experiment Design - Grade 9", "Cross-Disciplinary", Guid.Parse("74000000-0000-0000-0000-000000000008")),
                    ("Evidence-Based Reasoning - Grade 9", "Cross-Disciplinary", Guid.Parse("74000000-0000-0000-0000-000000000009")),
                    ("Scientific Reflection - Grade 8", "Cross-Disciplinary", Guid.Parse("74000000-0000-0000-0000-000000000010"))
                };

                var tagIds = new List<Guid>(tagSeeds.Length);
                foreach (var tag in tagSeeds)
                {
                    tagIds.Add(await EnsureTagAsync(db, tag.Label, tag.Subject, tag.FallbackId, ct));
                }

                var fullIlsId = await EnsureIlsAsync(
                    db,
                    title: "UAT Full ILS - Chemistry Density Investigation",
                    fallbackId: Guid.Parse("75000000-0000-0000-0000-000000000001"),
                    objective: "Students investigate how density, volume, and temperature impact experiment output.",
                    grade: "Grade 8",
                    durationMinutes: 35,
                    simulationId: chemistrySimId,
                    pollOptionsJson: JsonSerializer.Serialize(
                        new[] { "Strongly Disagree", "Disagree", "Agree", "Strongly Agree" }),
                    assessmentConfigJson: BuildAssessmentConfigJson("chem"),
                    status: IlsStatus.Published,
                    createdBy: teacherOneId,
                    createdAt: now.AddDays(-10),
                    ct);

                var optionalIlsId = await EnsureIlsAsync(
                    db,
                    title: "UAT Optional ILS - Physics Motion Challenge",
                    fallbackId: Guid.Parse("75000000-0000-0000-0000-000000000002"),
                    objective: "Students evaluate force and motion variables across multiple trials.",
                    grade: "Grade 9",
                    durationMinutes: 30,
                    simulationId: physicsSimId,
                    pollOptionsJson: null,
                    assessmentConfigJson: BuildAssessmentConfigJson("physics"),
                    status: IlsStatus.Published,
                    createdBy: teacherTwoId,
                    createdAt: now.AddDays(-10),
                    ct);

                await ReplaceIlsTagsAsync(db, fullIlsId, tagIds.Take(5).ToArray(), ct);
                await ReplaceIlsTagsAsync(db, optionalIlsId, tagIds.Skip(5).ToArray(), ct);

                await RebuildCompletedSessionAsync(
                    db,
                    sessionId: Guid.Parse("76000000-0000-0000-0000-000000000001"),
                    studentId: studentIds[0],
                    ilsId: fullIlsId,
                    hypothesisText: "If density increases while keeping volume stable, then the output score should rise.",
                    reflectionText: "The graph showed a clear trend when I changed density and temperature together.",
                    assessmentScore: 0.9m,
                    assessmentFeedback: "Great work linking the variable changes to your observations.",
                    startAt: now.AddDays(-2),
                    ct);

                await RebuildCompletedSessionAsync(
                    db,
                    sessionId: Guid.Parse("76000000-0000-0000-0000-000000000002"),
                    studentId: studentIds[1],
                    ilsId: optionalIlsId,
                    hypothesisText: "If force increases and friction drops, then acceleration should increase.",
                    reflectionText: "My final trial matched my hypothesis once I reduced friction.",
                    assessmentScore: 0.8m,
                    assessmentFeedback: "Solid result. Continue improving explanation depth on short answers.",
                    startAt: now.AddDays(-2).AddHours(1),
                    ct);

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                logger.LogInformation(
                    "UAT seed completed. teachers=2 students=5 simulations=3 tags=10 ils=2 completedSessions=2 biologySimulationId={BiologySimulationId}",
                    biologySimId);
            });
        }

        private static async Task<Guid> ResolveRoleIdAsync(
            BlueSandsLMSDbContext db,
            string roleName,
            Guid fallbackId,
            CancellationToken ct)
        {
            var existingId = await db.Roles
                .AsNoTracking()
                .Where(x => x.Name == roleName)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            return existingId == Guid.Empty ? fallbackId : existingId;
        }

        private static async Task<Guid> EnsureUserAsync(
            BlueSandsLMSDbContext db,
            string email,
            string fullName,
            Guid roleId,
            Guid fallbackId,
            DateTime createdAt,
            CancellationToken ct)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);
            if (user == null)
            {
                user = new User
                {
                    Id = fallbackId,
                    Email = email,
                    FullName = fullName,
                    PasswordHash = SharedPasswordHash,
                    RoleId = roleId,
                    IsActive = true,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    DateCreated = createdAt
                };

                db.Users.Add(user);
                return user.Id;
            }

            user.FullName = fullName;
            user.RoleId = roleId;
            user.PasswordHash = SharedPasswordHash;
            user.IsActive = true;
            user.IsEmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            return user.Id;
        }

        private static async Task<Guid> EnsureSimulationAsync(
            BlueSandsLMSDbContext db,
            string title,
            Guid fallbackId,
            string subject,
            string previewUrl,
            string runUrl,
            object config,
            CancellationToken ct)
        {
            var sim = await db.PhETSimulations.FirstOrDefaultAsync(x => x.Title == title, ct);
            if (sim == null)
            {
                sim = new PhETSimulation
                {
                    Id = fallbackId,
                    Title = title
                };
                db.PhETSimulations.Add(sim);
            }

            sim.IsActive = true;
            sim.ThumbnailUrl = previewUrl;
            sim.RunnableResource = runUrl;
            sim.SimPage = runUrl;
            sim.SimulationUrl = runUrl;
            sim.Type = "PhET-HTML5";
            sim.Topic = subject;
            sim.GradeLevel = "7-10";
            sim.LowGradeLevel = "7";
            sim.HighGradeLevel = "10";
            sim.Chemistry = string.Equals(subject, "chemistry", StringComparison.OrdinalIgnoreCase);
            sim.Biology = string.Equals(subject, "biology", StringComparison.OrdinalIgnoreCase);
            sim.Physics = string.Equals(subject, "physics", StringComparison.OrdinalIgnoreCase);
            sim.EarthSpace = false;
            sim.MathStatistics = false;
            sim.SimString = title.Replace(' ', '-').ToLowerInvariant();
            sim.TeacherTipsDoc = runUrl;
            sim.PdfUrl = runUrl;
            sim.CheerpJRunnable = runUrl;
            sim.Filename = $"{sim.SimString}.html";
            sim.ScreenNames = "Main";
            sim.MainTopics = subject;
            sim.Keywords = $"{subject},uat,simulation";
            sim.Standards = "NGSS";
            sim.SampleLearningGoals = "Understand variable relationships through repeated trials.";
            sim.Translations = "en";
            sim.Published = "2026";
            sim.LearningGoals = JsonSerializer.Serialize(config, JsonOptions);
            sim.Description = $"UAT seeded simulation for {subject}.";
            sim.LastUpdated = DateTime.UtcNow;
            return sim.Id;
        }

        private static async Task<Guid> EnsureTagAsync(
            BlueSandsLMSDbContext db,
            string label,
            string subject,
            Guid fallbackId,
            CancellationToken ct)
        {
            var tag = await db.CurriculumTags
                .FirstOrDefaultAsync(x => x.Label == label && x.Subject == subject, ct);

            if (tag == null)
            {
                tag = new CurriculumTag
                {
                    Id = fallbackId,
                    Label = label,
                    Subject = subject,
                    CreatedAt = DateTime.UtcNow
                };
                db.CurriculumTags.Add(tag);
            }
            else
            {
                tag.Label = label;
                tag.Subject = subject;
            }

            return tag.Id;
        }

        private static async Task<Guid> EnsureIlsAsync(
            BlueSandsLMSDbContext db,
            string title,
            Guid fallbackId,
            string objective,
            string grade,
            int durationMinutes,
            Guid simulationId,
            string? pollOptionsJson,
            string? assessmentConfigJson,
            IlsStatus status,
            Guid createdBy,
            DateTime createdAt,
            CancellationToken ct)
        {
            var ils = await db.InteractiveLearningSpaces
                .FirstOrDefaultAsync(x => x.Title == title, ct);
            if (ils == null)
            {
                ils = new InteractiveLearningSpace
                {
                    Id = fallbackId,
                    Title = title,
                    CreatedAt = createdAt
                };
                db.InteractiveLearningSpaces.Add(ils);
            }

            ils.Objective = objective;
            ils.Grade = grade;
            ils.DurationMinutes = durationMinutes;
            ils.SimulationId = simulationId;
            ils.PollOptionsJson = pollOptionsJson;
            ils.AssessmentConfigJson = assessmentConfigJson;
            ils.Status = status;
            ils.CreatedBy = createdBy;
            ils.UpdatedAt = DateTime.UtcNow;
            return ils.Id;
        }

        private static async Task ReplaceIlsTagsAsync(
            BlueSandsLMSDbContext db,
            Guid ilsId,
            IReadOnlyCollection<Guid> tagIds,
            CancellationToken ct)
        {
            await db.IlsTags
                .Where(x => x.IlsId == ilsId)
                .ExecuteDeleteAsync(ct);

            foreach (var tagId in tagIds)
            {
                db.IlsTags.Add(new IlsTag
                {
                    IlsId = ilsId,
                    TagId = tagId
                });
            }
        }

        private static async Task RebuildCompletedSessionAsync(
            BlueSandsLMSDbContext db,
            Guid sessionId,
            Guid studentId,
            Guid ilsId,
            string hypothesisText,
            string reflectionText,
            decimal assessmentScore,
            string assessmentFeedback,
            DateTime startAt,
            CancellationToken ct)
        {
            var existingSessionIds = await db.StudentIlsSessions
                .Where(x => (x.StudentId == studentId && x.IlsId == ilsId) || x.Id == sessionId)
                .Select(x => x.Id)
                .ToListAsync(ct);

            if (existingSessionIds.Count > 0)
            {
                await db.IlsDiscussionMessages.Where(x => existingSessionIds.Contains(x.SessionId)).ExecuteDeleteAsync(ct);
                await db.SessionAssessments.Where(x => existingSessionIds.Contains(x.SessionId)).ExecuteDeleteAsync(ct);
                await db.SessionReflections.Where(x => existingSessionIds.Contains(x.SessionId)).ExecuteDeleteAsync(ct);
                await db.SessionTrials.Where(x => existingSessionIds.Contains(x.SessionId)).ExecuteDeleteAsync(ct);
                await db.SessionHypotheses.Where(x => existingSessionIds.Contains(x.SessionId)).ExecuteDeleteAsync(ct);
                await db.SessionPolls.Where(x => existingSessionIds.Contains(x.SessionId)).ExecuteDeleteAsync(ct);
                await db.StudentIlsSessions.Where(x => existingSessionIds.Contains(x.Id)).ExecuteDeleteAsync(ct);
            }

            var completedAt = startAt.AddMinutes(25);
            var session = new StudentIlsSession
            {
                Id = sessionId,
                StudentId = studentId,
                IlsId = ilsId,
                CurrentStep = 7,
                ReflectionSubmitted = true,
                BadgeAwarded = true,
                CompletedAt = completedAt,
                CreatedAt = startAt,
                UpdatedAt = completedAt
            };

            db.StudentIlsSessions.Add(session);

            db.SessionPolls.Add(new SessionPoll
            {
                Id = DeterministicGuid(sessionId, "poll"),
                SessionId = sessionId,
                OptionIndex = 2,
                SubmittedAt = startAt.AddMinutes(1)
            });

            db.SessionHypotheses.Add(new SessionHypothesis
            {
                Id = DeterministicGuid(sessionId, "hypothesis"),
                SessionId = sessionId,
                Text = hypothesisText,
                InputMethod = "text",
                SubmittedAt = startAt.AddMinutes(3)
            });

            var trials = new[]
            {
                new { Density = 1.0, Volume = 150.0, Temp = 22.0, Observation = "Initial baseline trial." },
                new { Density = 1.2, Volume = 180.0, Temp = 24.0, Observation = "Output increased as density rose." },
                new { Density = 1.35, Volume = 210.0, Temp = 27.0, Observation = "Final trial confirmed the expected trend." }
            };

            for (var i = 0; i < trials.Length; i++)
            {
                var score = Math.Round(
                    (trials[i].Density * trials[i].Volume) / Math.Max(1d, Math.Abs(trials[i].Temp) + 1d),
                    4);

                db.SessionTrials.Add(new SessionTrial
                {
                    Id = DeterministicGuid(sessionId, $"trial-{i + 1}"),
                    SessionId = sessionId,
                    VariablesJson = JsonSerializer.Serialize(new
                    {
                        density = trials[i].Density,
                        volume = trials[i].Volume,
                        temp = trials[i].Temp
                    }),
                    ObservationText = trials[i].Observation,
                    ResultJson = JsonSerializer.Serialize(new
                    {
                        score,
                        feedback = "Seeded UAT trial response."
                    }),
                    CreatedAt = startAt.AddMinutes(6 + (i * 3))
                });
            }

            db.SessionReflections.Add(new SessionReflection
            {
                Id = DeterministicGuid(sessionId, "reflection"),
                SessionId = sessionId,
                Text = reflectionText,
                SubmittedAt = startAt.AddMinutes(18)
            });

            db.SessionAssessments.Add(new SessionAssessment
            {
                Id = DeterministicGuid(sessionId, "assessment"),
                SessionId = sessionId,
                AnswersJson = JsonSerializer.Serialize(new[]
                {
                    new { qId = "q1", value = "A" },
                    new { qId = "q2", value = "Density and temperature changed the output trend." }
                }),
                Score = assessmentScore,
                Feedback = assessmentFeedback,
                IsAutoSubmitted = false,
                SubmittedAt = completedAt
            });
        }

        private static string BuildAssessmentConfigJson(string domain)
        {
            if (string.Equals(domain, "physics", StringComparison.OrdinalIgnoreCase))
            {
                return JsonSerializer.Serialize(new
                {
                    questions = new object[]
                    {
                        new
                        {
                            qId = "q1",
                            type = "mcq",
                            points = 1,
                            correctAnswer = "A",
                            feedback = "Recheck how force and friction change acceleration."
                        },
                        new
                        {
                            qId = "q2",
                            type = "short",
                            points = 1,
                            expectedKeywords = new[] { "force", "friction", "acceleration" },
                            feedback = "Mention force, friction, and acceleration in your explanation."
                        }
                    }
                });
            }

            return JsonSerializer.Serialize(new
            {
                questions = new object[]
                {
                    new
                    {
                        qId = "q1",
                        type = "mcq",
                        points = 1,
                        correctAnswer = "A",
                        feedback = "Revisit the trial graph before selecting an answer."
                    },
                    new
                    {
                        qId = "q2",
                        type = "short",
                        points = 1,
                        expectedKeywords = new[] { "density", "temperature", "volume" },
                        feedback = "Include all key variables from your trials."
                    },
                    new
                    {
                        qId = "q3",
                        type = "mcq",
                        points = 1,
                        correctAnswer = "C",
                        feedback = "Review how temperature changed molecule movement."
                    }
                }
            });
        }

        private static Guid DeterministicGuid(Guid root, string suffix)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes($"{root:D}:{suffix}"));

            Span<byte> guidBytes = stackalloc byte[16];
            bytes.AsSpan(0, 16).CopyTo(guidBytes);
            return new Guid(guidBytes);
        }
    }
}
