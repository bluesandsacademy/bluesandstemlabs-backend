using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

const string Password = "Test@1234";
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var baseUrl = Arg("--base-url") ?? Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5055";
var reportPath = Path.Combine(root, "BlueSandsLMS_Sprint_Completion_Report.docx");

var config = new ConfigurationBuilder()
    .SetBasePath(root)
    .AddJsonFile("BlueSandsLMS.Api/appsettings.json", optional: true)
    .AddJsonFile("BlueSandsLMS.Api/appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required.");

var state = new TestState();
var results = new List<TestResult>();

var dbOptions = new DbContextOptionsBuilder<BlueSandsLMSDbContext>()
    .UseSqlServer(cs, sql => sql.EnableRetryOnFailure())
    .Options;

await using (var db = new BlueSandsLMSDbContext(dbOptions))
{
    await db.Database.MigrateAsync();
    await SeedAsync(db);
}

using var http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
http.Timeout = TimeSpan.FromSeconds(60);

async Task MainRun()
{
    await AcquireTokensAsync();

    await Run(1, "POST", "/api/auth/register", null, HttpStatusCode.OK, new
    {
        fullName = "E2E Registered Student",
        email = $"e2e.register.{Guid.NewGuid():N}@bluesandstemlabs.com",
        password = Password,
        phone = "08000000000",
        country = "Nigeria"
    });

    await Run(2, "POST", "/api/auth/register-school", null, HttpStatusCode.OK, new
    {
        fullName = "E2E Registered Admin",
        email = $"e2e.school.{Guid.NewGuid():N}@bluesandstemlabs.com",
        phone = "08000000001",
        position = "Administrator",
        totalStudents = 25,
        country = "Nigeria",
        password = Password,
        schoolName = $"E2E Registered School {Guid.NewGuid():N}"[..28],
        subdomain = $"e2e-{Guid.NewGuid():N}"[..12]
    });

    var login = await Run(3, "POST", "/api/auth/login", null, HttpStatusCode.OK, new
    {
        email = "testuser@bluesandstemlabs.com",
        password = Password
    });
    state.IndividualToken = ExtractToken(login.Body) ?? state.IndividualToken;

    await Run(4, "POST", "/api/auth/google-signin", null, HttpStatusCode.OK, new { idToken = "e2e-google-token" });
    await Run(5, "POST", "/api/auth/change-password", state.TeacherToken, HttpStatusCode.OK, new { currentPassword = Password, newPassword = "Test@12345" });
    await Run(6, "POST", "/api/auth/change-password", state.StudentToken, HttpStatusCode.OK, new { currentPassword = Password, newPassword = "Test@12345" });
    await Run(7, "GET", "/api/school-admin/v2/profile", state.SchoolAdminToken, HttpStatusCode.OK);
    await Run(8, "POST", "/api/auth/forgot-password", null, HttpStatusCode.OK, new { email = "testuser@bluesandstemlabs.com" });
    await Run(9, "POST", "/api/auth/reset-password", null, HttpStatusCode.OK, new { token = "e2e-reset-token", newPassword = "Test@12345" });
    await Run(10, "POST", "/api/auth/resend-verification", null, HttpStatusCode.OK, new { email = "testuser@bluesandstemlabs.com" });

    var createTeacher = await Run(11, "POST", "/api/school-admin/teachers", state.SchoolAdminToken, HttpStatusCode.OK, new { email = "testteacher@bluesandstemlabs.com", fullName = "Test Teacher", phone = "08000000002", country = "Nigeria" });
    state.TeacherId = ExtractGuid(createTeacher.Body, "userId") ?? state.TeacherId;
    await Run(12, "GET", "/api/school-admin/teachers", state.SchoolAdminToken, HttpStatusCode.OK);
    await Run(13, "POST", $"/api/classes/{state.ClassId}/students", state.TeacherToken, HttpStatusCode.OK, new { email = "teststudent@bluesandstemlabs.com" });
    await Run(14, "GET", $"/api/classes/{state.ClassId}/students", state.TeacherToken, HttpStatusCode.OK);
    await Run(15, "POST", "/api/school-admin/roles/assign", state.SchoolAdminToken, HttpStatusCode.OK, new { userId = state.TeacherId, role = "Teacher" });
    await Run(16, "GET", "/api/school-admin/roles", state.SchoolAdminToken, HttpStatusCode.OK);

    await Run(17, "GET", "/api/teacher/dashboard", state.TeacherToken, HttpStatusCode.OK);
    await Run(18, "GET", "/api/student/v1/dashboard", state.StudentToken, HttpStatusCode.OK);
    await Run(19, "GET", "/api/school-admin/analytics", state.SchoolAdminToken, HttpStatusCode.OK);
    await Run(20, "GET", "/api/exports/engagement", state.TeacherToken, HttpStatusCode.OK);
    await Run(21, "GET", "/api/teacher/performance-metrics", state.TeacherToken, HttpStatusCode.OK);
    await Run(22, "GET", "/api/student/v1/experiments", state.StudentToken, HttpStatusCode.OK);
    await Run(23, "GET", "/api/student/v1/assessments/summary", state.StudentToken, HttpStatusCode.OK);
    await Run(24, "GET", "/api/school-admin/billing", state.SchoolAdminToken, HttpStatusCode.OK);
    await Run(25, "GET", "/api/school-admin/billing/plans", state.SchoolAdminToken, HttpStatusCode.OK);
    await Run(26, "GET", "/api/student/v1/assessments", state.StudentToken, HttpStatusCode.OK);

    await Run(27, "POST", "/api/payments/initiate", state.SchoolAdminToken, HttpStatusCode.OK, new { schoolId = state.SchoolId, students = 10, contactEmail = "testschooladmin@bluesandstemlabs.com", promoCode = "E2E-VALID" });
    var feedback = await Run(28, "POST", "/api/feedback", state.StudentToken, HttpStatusCode.OK, new { message = "Student E2E feedback", category = "General" });
    state.FeedbackId = ExtractGuid(feedback.Body, "id") ?? state.FeedbackId;
    await Run(29, "POST", "/api/feedback", state.TeacherToken, HttpStatusCode.OK, new { message = "Teacher E2E feedback", category = "General" });
    await Run(30, "POST", "/api/feedback", state.SchoolAdminToken, HttpStatusCode.OK, new { message = "School admin E2E feedback", category = "General" });
    await Run(31, "GET", "/api/admin/feedback", state.GlobalAdminToken, HttpStatusCode.OK);
    await Run(32, "PATCH", $"/api/admin/feedback/{state.FeedbackId}/status", state.GlobalAdminToken, HttpStatusCode.OK, new { status = "Reviewed" });
    await Run(33, "GET", $"/api/student/v1/experiments/launch/{state.LockedSimulationId}", state.IndividualToken, HttpStatusCode.Forbidden);
    await Run(34, "GET", $"/api/student/v1/experiments/launch/{state.LockedSimulationId}", state.StudentToken, HttpStatusCode.OK);

    await Run(35, "POST", "/api/support/ticket", state.TeacherToken, HttpStatusCode.OK, new { subject = "Teacher support", message = "Teacher support request", category = "Technical" });
    await Run(36, "POST", "/api/support/ticket", state.StudentToken, HttpStatusCode.OK, new { subject = "Student support", message = "Student support request", category = "Content" });
    await Run(37, "POST", "/api/support/ticket", state.SchoolAdminToken, HttpStatusCode.OK, new { subject = "School admin support", message = "School admin support request", category = "Billing" });
    await Run(38, "GET", "/api/support/resources", state.TeacherToken, HttpStatusCode.OK);
    await Run(39, "GET", "/api/reports/teacher", state.TeacherToken, HttpStatusCode.OK);
    await Run(40, "GET", "/api/reports/teacher/export/csv", state.TeacherToken, HttpStatusCode.OK, expectCsv: true);
    await Run(41, "GET", "/api/reports/student", state.StudentToken, HttpStatusCode.OK);
    await Run(42, "GET", "/api/reports/student/export/csv", state.StudentToken, HttpStatusCode.OK, expectCsv: true);
    await Run(43, "GET", "/api/reports/school", state.SchoolAdminToken, HttpStatusCode.OK);
    await Run(44, "GET", "/api/reports/school/export/csv", state.SchoolAdminToken, HttpStatusCode.OK, expectCsv: true);
    await Run(45, "GET", "/api/teacher/communication-metrics", state.TeacherToken, HttpStatusCode.OK);
    await Run(46, "GET", "/api/reports/analytics", state.SchoolAdminToken, HttpStatusCode.OK);
    await Run(47, "GET", "/api/leaderboard/teachers", state.SchoolAdminToken, HttpStatusCode.OK);
    await Run(48, "GET", "/api/shop/products", null, HttpStatusCode.OK);
    var order = await Run(49, "POST", "/api/shop/orders", state.StudentToken, HttpStatusCode.OK, new { productId = state.ProductId, quantity = 1 });
    state.OrderId = ExtractGuid(order.Body, "id") ?? state.OrderId;
    await Run(50, "GET", $"/api/shop/orders/{state.OrderId}", state.StudentToken, HttpStatusCode.OK);
    await Run(51, "GET", "/api/admin/shop/orders", state.GlobalAdminToken, HttpStatusCode.OK);
    await Run(52, "GET", "/health/live", null, HttpStatusCode.OK);
    await Run(53, "GET", "/health/ready", null, HttpStatusCode.OK);

    PrintSummary();
    WriteDocx(reportPath, results);
    Console.WriteLine($"Report: {reportPath}");
}

await MainRun();

string? Arg(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

async Task AcquireTokensAsync()
{
    state.IndividualToken = await LoginToken("testuser@bluesandstemlabs.com");
    state.TeacherToken = await LoginToken("testteacher@bluesandstemlabs.com");
    state.StudentToken = await LoginToken("teststudent@bluesandstemlabs.com");
    state.SchoolAdminToken = await LoginToken("testschooladmin@bluesandstemlabs.com");
    state.GlobalAdminToken = await LoginToken("testglobaladmin@bluesandstemlabs.com");
}

async Task<string> LoginToken(string email)
{
    using var response = await http.PostAsync("api/auth/login", Json(new { email, password = Password }));
    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
        throw new InvalidOperationException($"Login failed for {email}: {(int)response.StatusCode} {body}");

    return ExtractToken(body) ?? throw new InvalidOperationException($"Login response did not include token for {email}: {body}");
}

async Task<TestResult> Run(int number, string method, string path, string? token, HttpStatusCode expected, object? body = null, bool expectCsv = false)
{
    using var request = new HttpRequestMessage(new HttpMethod(method), path.TrimStart('/'));
    if (token != null)
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (body != null)
        request.Content = Json(body);

    using var response = await http.SendAsync(request);
    var text = await response.Content.ReadAsStringAsync();
    var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
    var errorEnvelope = IsErrorEnvelope(text);
    var passed = response.StatusCode == expected &&
                 (!expectCsv || contentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase)) &&
                 ((int)expected >= 400 || !errorEnvelope);

    var result = new TestResult(number, method, path, (int)expected, (int)response.StatusCode, contentType, Summarize(text), text, passed);
    results.Add(result);

    Console.WriteLine($"{(passed ? "[PASS]" : "[FAIL]")} #{number} {method} {path} -> {(int)response.StatusCode}");
    if (!passed)
        Console.WriteLine($"       expected {(int)expected}, content-type={contentType}, body={result.BodySummary}");

    return result;
}

static StringContent Json(object body)
    => new(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json");

static string? ExtractToken(string body)
{
    using var doc = JsonDocument.Parse(body);
    return doc.RootElement.TryGetProperty("token", out var token)
        ? token.GetString()
        : doc.RootElement.TryGetProperty("accessToken", out var accessToken)
            ? accessToken.GetString()
            : null;
}

static Guid? ExtractGuid(string body, string property)
{
    try
    {
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty(property, out var element) &&
            Guid.TryParse(element.GetString(), out var id))
            return id;
    }
    catch
    {
        return null;
    }

    return null;
}

static bool IsErrorEnvelope(string body)
{
    if (string.IsNullOrWhiteSpace(body)) return false;
    try
    {
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.ValueKind == JsonValueKind.Object &&
               doc.RootElement.TryGetProperty("code", out _) &&
               doc.RootElement.TryGetProperty("message", out _);
    }
    catch
    {
        return false;
    }
}

static string Summarize(string body)
{
    if (string.IsNullOrWhiteSpace(body)) return "";
    var normalized = Regex.Replace(body, "\\s+", " ").Trim();
    return normalized.Length <= 240 ? normalized : normalized[..240] + "...";
}

void PrintSummary()
{
    var passed = results.Count(x => x.Passed);
    var failed = results.Count - passed;
    Console.WriteLine();
    Console.WriteLine($"Final test summary:");
    Console.WriteLine($"Total: {results.Count} | Passed: {passed} | Failed: {failed}");
    if (failed > 0)
    {
        Console.WriteLine("Failed tests:");
        foreach (var fail in results.Where(x => !x.Passed))
            Console.WriteLine($"#{fail.Number} {fail.Method} {fail.Endpoint} -> {fail.ActualStatus} (expected {fail.ExpectedStatus})");
    }
}

async Task SeedAsync(BlueSandsLMSDbContext db)
{
    var now = DateTime.UtcNow;
    var roles = new Dictionary<string, Guid>
    {
        ["Student"] = await EnsureRole(db, "Student"),
        ["Teacher"] = await EnsureRole(db, "Teacher"),
        ["SchoolAdmin"] = await EnsureRole(db, "SchoolAdmin"),
        ["GlobalAdmin"] = await EnsureRole(db, "GlobalAdmin"),
        ["Admin"] = await EnsureRole(db, "Admin")
    };

    state.SchoolId = Guid.Parse("91000000-0000-0000-0000-000000000001");
    var school = await db.Schools.FirstOrDefaultAsync(x => x.Id == state.SchoolId);
    if (school == null)
    {
        school = new School
        {
            Id = state.SchoolId,
            Name = "Blue Sands E2E School",
            Subdomain = "blue-sands-e2e",
            IsActive = true,
            DateCreated = now,
            Country = "Nigeria",
            Currency = "NGN",
            TotalStudents = 100,
            ContactName = "Test School Admin",
            ContactEmail = "testschooladmin@bluesandstemlabs.com",
            ContactPhone = "08000000000",
            ContactPosition = "Administrator"
        };
        db.Schools.Add(school);
    }

    var passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);
    state.IndividualId = await EnsureUser("testuser@bluesandstemlabs.com", "Test Individual User", roles["Student"], null, passwordHash);
    state.TeacherId = await EnsureUser("testteacher@bluesandstemlabs.com", "Test Teacher", roles["Teacher"], state.SchoolId, passwordHash);
    state.StudentId = await EnsureUser("teststudent@bluesandstemlabs.com", "Test Student", roles["Student"], state.SchoolId, passwordHash);
    state.SchoolAdminId = await EnsureUser("testschooladmin@bluesandstemlabs.com", "Test School Admin", roles["SchoolAdmin"], state.SchoolId, passwordHash);
    state.GlobalAdminId = await EnsureUser("testglobaladmin@bluesandstemlabs.com", "Test Global Admin", roles["GlobalAdmin"], null, passwordHash);

    state.ClassId = Guid.Parse("92000000-0000-0000-0000-000000000001");
    var classroom = await db.Classrooms.FirstOrDefaultAsync(x => x.Id == state.ClassId);
    if (classroom == null)
    {
        db.Classrooms.Add(new Classroom
        {
            Id = state.ClassId,
            SchoolId = state.SchoolId,
            Name = "E2E Physics",
            Subject = "Physics",
            CreatedAt = now.AddDays(-10)
        });
    }

    await EnsureClassTeacher(state.ClassId, state.TeacherId);
    await EnsureEnrollment(state.ClassId, state.TeacherId, ClassRole.Teacher);
    await EnsureEnrollment(state.ClassId, state.StudentId, ClassRole.Student);

    state.LockedSimulationId = Guid.Parse("93000000-0000-0000-0000-000000000001");
    var sim = await db.PhETSimulations.FirstOrDefaultAsync(x => x.Id == state.LockedSimulationId);
    if (sim == null)
    {
        sim = new PhETSimulation { Id = state.LockedSimulationId, Title = "E2E Locked Simulation" };
        db.PhETSimulations.Add(sim);
    }
    sim.IsActive = true;
    sim.IsFree = false;
    sim.RunnableResource = "https://cdn.bluesands.local/e2e/simulation.html";
    sim.SimulationUrl = sim.RunnableResource;
    sim.Topic = "Physics";

    await EnsureSubscription(state.SchoolId, null, "TRIAL", true, now.AddDays(-1), now.AddDays(13));
    await RemoveIndividualSubscriptions(state.IndividualId);
    await EnsurePromoCode("E2E-VALID");
    await EnsurePricingTier();
    await EnsureSupportAndShopSeeds();
    await EnsureLearningData(now);
    await EnsureResetToken(state.IndividualId, "e2e-reset-token");

    await db.SaveChangesAsync();

    async Task<Guid> EnsureUser(string email, string fullName, Guid roleId, Guid? schoolId, string hash)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            user = new User { Id = Guid.NewGuid(), Email = email };
            db.Users.Add(user);
        }

        user.FullName = fullName;
        user.PasswordHash = hash;
        user.RoleId = roleId;
        user.SchoolId = schoolId;
        user.IsActive = true;
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = now;
        user.DateCreated = user.DateCreated == default ? now.AddDays(-20) : user.DateCreated;
        user.Phone = "08000000000";
        user.Country = "Nigeria";
        return user.Id;
    }

    async Task EnsureClassTeacher(Guid classroomId, Guid teacherId)
    {
        if (!await db.ClassroomTeachers.AnyAsync(x => x.ClassroomId == classroomId && x.TeacherUserId == teacherId))
            db.ClassroomTeachers.Add(new ClassroomTeacher { ClassroomId = classroomId, TeacherUserId = teacherId, AssignedAt = now.AddDays(-9) });
    }

    async Task EnsureEnrollment(Guid classroomId, Guid userId, ClassRole role)
    {
        var enrollment = await db.Enrollments.FirstOrDefaultAsync(x => x.ClassroomId == classroomId && x.UserId == userId);
        if (enrollment == null)
        {
            db.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), ClassroomId = classroomId, UserId = userId, RoleInClass = role, CreatedAt = now.AddDays(-9) });
        }
        else
        {
            enrollment.RoleInClass = role;
        }
    }

    async Task EnsureSubscription(Guid schoolId, Guid? userId, string reference, bool active, DateTime starts, DateTime ends)
    {
        var sub = await db.Subscriptions.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.UserId == userId && x.LastPaymentReference == reference);
        if (sub == null)
        {
            db.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                UserId = userId,
                StudentsCovered = 100,
                PricePerStudent = 0,
                StartsAt = starts,
                EndsAt = ends,
                Active = active,
                LastPaymentReference = reference
            });
        }
        else
        {
            sub.Active = active;
            sub.StartsAt = starts;
            sub.EndsAt = ends;
        }
    }

    async Task RemoveIndividualSubscriptions(Guid userId)
    {
        var subs = await db.Subscriptions.Where(x => x.UserId == userId).ToListAsync();
        db.Subscriptions.RemoveRange(subs);
    }

    async Task EnsurePromoCode(string code)
    {
        var promo = await db.PromoCodes.FirstOrDefaultAsync(x => x.Code == code);
        if (promo == null)
        {
            db.PromoCodes.Add(new PromoCode { Id = Guid.NewGuid(), Code = code, IsActive = true, ExpiresAt = now.AddYears(1), MaxRedemptions = null });
        }
        else
        {
            promo.IsActive = true;
            promo.ExpiresAt = now.AddYears(1);
            promo.MaxRedemptions = null;
        }
    }

    async Task EnsurePricingTier()
    {
        if (!await db.PricingTiers.AnyAsync())
        {
            db.PricingTiers.Add(new PricingTier { Id = Guid.NewGuid(), TierName = "E2E Standard", MinStudents = 1, MaxStudents = 500, PricePerStudent = 2500m });
        }
    }

    async Task EnsureSupportAndShopSeeds()
    {
        if (!await db.SupportResources.AnyAsync())
        {
            db.SupportResources.AddRange(
                new SupportResource { Id = Guid.NewGuid(), Title = "E2E Onboarding", Description = "Onboarding resource", Url = "https://www.bluesandstemlabs.com/help/onboarding", Category = "Onboarding", CreatedAt = now },
                new SupportResource { Id = Guid.NewGuid(), Title = "E2E Teaching", Description = "Teaching resource", Url = "https://www.bluesandstemlabs.com/help/teaching", Category = "Teaching", CreatedAt = now },
                new SupportResource { Id = Guid.NewGuid(), Title = "E2E Billing", Description = "Billing resource", Url = "https://www.bluesandstemlabs.com/help/billing", Category = "Billing", CreatedAt = now });
        }

        var product = await db.Products.FirstOrDefaultAsync(x => x.Name == "E2E Lab Kit");
        if (product == null)
        {
            product = new Product { Id = Guid.NewGuid(), Name = "E2E Lab Kit", Description = "E2E test product", Price = 1000m, Currency = "NGN", Category = "Lab Kits", StockCount = 100, IsActive = true, CreatedAt = now };
            db.Products.Add(product);
        }
        product.StockCount = Math.Max(product.StockCount, 25);
        product.IsActive = true;
        state.ProductId = product.Id;
    }

    async Task EnsureLearningData(DateTime utcNow)
    {
        if (!await db.QuizAttempts.AnyAsync(x => x.UserId == state.StudentId && x.QuizCode == "E2E-Q1"))
        {
            db.QuizAttempts.Add(new QuizAttempt
            {
                Id = Guid.NewGuid(),
                UserId = state.StudentId,
                ClassroomId = state.ClassId,
                Subject = "Physics",
                QuizCode = "E2E-Q1",
                Passed = true,
                Score0to1 = 0.86m,
                StartedAt = utcNow.AddDays(-3),
                CompletedAt = utcNow.AddDays(-3).AddMinutes(15),
                DateCreated = utcNow.AddDays(-3)
            });
        }

        if (!await db.ExperimentLaunches.AnyAsync(x => x.UserId == state.StudentId && x.ExperimentName == "E2E Locked Simulation"))
        {
            db.ExperimentLaunches.Add(new ExperimentLaunch
            {
                Id = Guid.NewGuid(),
                UserId = state.StudentId,
                ClassroomId = state.ClassId,
                PhETSimulationId = state.LockedSimulationId,
                ExperimentName = "E2E Locked Simulation",
                Subject = "Physics",
                Mode = "guided",
                StartedAt = utcNow.AddDays(-2),
                EndedAt = utcNow.AddDays(-2).AddMinutes(20),
                DurationSec = 1200,
                Completed = true
            });
        }

        var assignmentId = Guid.Parse("94000000-0000-0000-0000-000000000001");
        if (!await db.Assignments.AnyAsync(x => x.Id == assignmentId))
        {
            db.Assignments.Add(new Assignment
            {
                Id = assignmentId,
                ClassroomId = state.ClassId,
                Title = "E2E Assignment",
                Type = BlueSandsLMS.Core.Entities.AssignmentType.Quiz,
                ResourceCode = "E2E-Q1",
                DueAt = utcNow.AddDays(2),
                CreatedByUserId = state.TeacherId,
                CreatedAt = utcNow.AddDays(-4)
            });
        }

        if (!await db.Submissions.AnyAsync(x => x.AssignmentId == assignmentId && x.StudentId == state.StudentId))
        {
            db.Submissions.Add(new Submission
            {
                Id = Guid.NewGuid(),
                AssignmentId = assignmentId,
                StudentId = state.StudentId,
                Status = SubmissionStatus.Graded,
                Score0to1 = 0.88m,
                SubmittedAt = utcNow.AddDays(-1),
                GradedAt = utcNow.AddHours(-12),
                GraderUserId = state.TeacherId,
                Feedback = "E2E seeded feedback"
            });
        }

        if (!await db.MessageLogs.AnyAsync(x => x.FromUserId == state.TeacherId && x.Body == "E2E teacher message"))
        {
            db.MessageLogs.Add(new MessageLog
            {
                Id = Guid.NewGuid(),
                FromUserId = state.TeacherId,
                ToUserId = state.StudentId,
                ClassroomId = state.ClassId,
                Body = "E2E teacher message",
                SentAt = utcNow.AddDays(-1),
                ReadAt = utcNow.AddHours(-20)
            });
        }

        if (!await db.Announcements.AnyAsync(x => x.CreatedByUserId == state.TeacherId && x.Title == "E2E Announcement"))
        {
            db.Announcements.Add(new Announcement
            {
                Id = Guid.NewGuid(),
                SchoolId = state.SchoolId,
                ClassroomId = state.ClassId,
                Title = "E2E Announcement",
                Body = "E2E announcement body",
                CreatedByUserId = state.TeacherId,
                CreatedAt = utcNow.AddDays(-1)
            });
        }
    }

    async Task EnsureResetToken(Guid userId, string plainToken)
    {
        var hash = Sha256Hex(plainToken);
        var existing = await db.PasswordResetTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
        if (existing == null)
        {
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = hash,
                CreatedAt = now,
                ExpiresAt = now.AddHours(1),
                IsUsed = false
            });
        }
        else
        {
            existing.UserId = userId;
            existing.ExpiresAt = now.AddHours(1);
            existing.IsUsed = false;
        }
    }
}

async Task<Guid> EnsureRole(BlueSandsLMSDbContext db, string name)
{
    var role = await db.Roles.FirstOrDefaultAsync(x => x.Name == name);
    if (role == null)
    {
        role = new Role { Id = Guid.NewGuid(), Name = name };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
    }

    return role.Id;
}

static string Sha256Hex(string input)
{
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(hash).ToLowerInvariant();
}

void WriteDocx(string path, IReadOnlyList<TestResult> testResults)
{
    if (File.Exists(path)) File.Delete(path);
    using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
    Add(zip, "[Content_Types].xml", ContentTypesXml());
    Add(zip, "_rels/.rels", RelsXml());
    Add(zip, "word/_rels/document.xml.rels", DocumentRelsXml());
    Add(zip, "word/styles.xml", StylesXml());
    Add(zip, "word/document.xml", DocumentXml(testResults));
}

static void Add(ZipArchive zip, string path, string content)
{
    var entry = zip.CreateEntry(path);
    using var stream = entry.Open();
    using var writer = new StreamWriter(stream, new UTF8Encoding(false));
    writer.Write(content);
}

static string Esc(string value) => WebUtility.HtmlEncode(value);

static string P(string text, string style = "Normal")
    => $"<w:p><w:pPr><w:pStyle w:val=\"{style}\"/></w:pPr><w:r><w:t>{Esc(text)}</w:t></w:r></w:p>";

static string Table(params string[][] rows)
{
    var sb = new StringBuilder("<w:tbl><w:tblPr><w:tblStyle w:val=\"TableGrid\"/><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr>");
    foreach (var row in rows)
    {
        sb.Append("<w:tr>");
        foreach (var cell in row)
            sb.Append("<w:tc><w:tcPr><w:tcW w:w=\"2400\" w:type=\"dxa\"/></w:tcPr>").Append(P(cell)).Append("</w:tc>");
        sb.Append("</w:tr>");
    }
    sb.Append("</w:tbl>");
    return sb.ToString();
}

static string ContentTypesXml() =>
    """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/><Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/></Types>""";

static string RelsXml() =>
    """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>""";

static string DocumentRelsXml() =>
    """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""";

static string StylesXml() =>
    """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:rPr><w:sz w:val="22"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:rPr><w:b/><w:color w:val="1E3A5F"/><w:sz w:val="36"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:rPr><w:b/><w:color w:val="1E3A5F"/><w:sz w:val="28"/></w:rPr></w:style><w:style w:type="table" w:styleId="TableGrid"><w:name w:val="Table Grid"/><w:tblPr><w:tblBorders><w:top w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:left w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:bottom w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:right w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:insideH w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:insideV w:val="single" w:sz="4" w:space="0" w:color="auto"/></w:tblBorders></w:tblPr></w:style></w:styles>""";

static string DocumentXml(IReadOnlyList<TestResult> testResults)
{
    var completedTasks = new[]
    {
        new[] { "Task ID", "Description", "Phase", "Priority", "Status", "Notes" },
        new[] { "BE-27", "Teacher support ticket submission", "6", "High", "Complete", "Stored in SupportTickets" },
        new[] { "BE-28", "Student support ticket submission", "6", "High", "Complete", "Same endpoint as teacher" },
        new[] { "BE-29", "School admin support ticket submission", "6", "High", "Complete", "UserType from JWT" },
        new[] { "BE-30", "Support resources endpoint", "6", "Medium", "Complete", "Seeded resources" },
        new[] { "BE-31", "Role-based reports and CSV exports", "6", "High", "Complete", "Teacher, student, school" },
        new[] { "BE-32", "Communication metrics, analytics, leaderboard", "6", "High", "Complete", "Teacher Forum deferred" },
        new[] { "NEW-23", "Shop backend", "6", "Medium", "Complete", "Products and orders" }
    };

    var testRows = testResults
        .Select(x => new[] { x.Number.ToString(), x.Method, x.Endpoint, x.ExpectedStatus.ToString(), x.ActualStatus.ToString(), x.Passed ? "PASS" : "FAIL" })
        .Prepend(new[] { "#", "Method", "Endpoint", "Expected", "Result", "Status" })
        .ToArray();

    var skipped = new[]
    {
        new[] { "Task ID", "Description", "Reason Skipped", "What Is Needed to Complete It" },
        new[] { "BE-18", "Paystack fraud fix", "Requires Paystack support contact, not a code fix", "Contact Paystack support team with transaction IDs" },
        new[] { "BE-24", "Zoho Desk integration", "Requires Zoho account and API keys", "Provide Zoho Desk subdomain, API token, and department ID" },
        new[] { "Teacher Forum", "BE-32 partial", "Full feature build out of sprint scope", "Dedicated sprint with UI/UX spec and backend design" },
        new[] { "NEW-09", "Free trial model", "Alero confirmation pending", "Alero to confirm 14-day vs lifetime model in writing" }
    };

    var body = new StringBuilder();
    body.Append(P("BlueSandsLMS Backend - Sprint Completion Report", "Title"));
    body.Append(P("1. Sprint Overview", "Heading1"));
    body.Append(P("Developer: Ifedayo Michael"));
    body.Append(P("Role: Backend Developer"));
    body.Append(P("Sprint dates: 5 - 23 May 2026 (delivered: June 2, 2026)"));
    body.Append(P("Total tasks: 38 | Critical: 10 | High: 16 | Medium: 12"));
    body.Append(P("2. Completed Tasks", "Heading1")).Append(Table(completedTasks));
    body.Append(P("3. Test Results", "Heading1")).Append(Table(testRows));
    body.Append(P("4. Migrations Applied", "Heading1"));
    body.Append(P("20250803090611_InitialCreate through 20260531202652_AddSupportAndShopTables were validated by EF Core migration state. Sprint schema changes include support tickets/resources, feedback fields, student estimates, PraxiLabs support, ILS phases, hardening indexes, and shop product/order tables."));
    body.Append(P("5. What Was Intentionally Skipped and Why", "Heading1")).Append(Table(skipped));
    body.Append(P("6. Security Actions Still Required", "Heading1"));
    foreach (var item in new[]
    {
        "Rotate DB password from bluesands1234 to a strong 32+ character password",
        "Rotate JWT secret to a new 64-character random string",
        "Rotate Gmail app password for noreply@bluesandstemlabs.com",
        "Rotate PraxiLabs API key and secret",
        "Add appsettings.production.json to the server via FTP with all real rotated credentials",
        "Fix space in App.BaseUrl currently breaking email verification links",
        "Add Google Client ID to production config for audience validation on Google Sign In",
        "Confirm appsettings.Development.json is git-ignored and never pushed"
    }) body.Append(P("[ ] " + item));
    body.Append(P("7. Recommended Next Steps", "Heading1"));
    foreach (var item in new[]
    {
        "Deploy to production and run smoke tests against live endpoints",
        "Complete Zoho Desk integration once API keys are provided",
        "Resolve Paystack bulk payment fraud denial with Paystack support",
        "Build Teacher Forum as a dedicated feature sprint",
        "Add integration tests for Auth and Payment flows",
        "Implement soft delete query filtering",
        "Standardize API response envelope across all controllers",
        "Add pagination to remaining list endpoints"
    }) body.Append(P("- " + item));

    return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>{body}<w:sectPr><w:pgSz w:w="12240" w:h="15840"/><w:pgMar w:top="720" w:right="720" w:bottom="720" w:left="720"/></w:sectPr></w:body></w:document>""";
}

sealed class TestState
{
    public string IndividualToken { get; set; } = "";
    public string TeacherToken { get; set; } = "";
    public string StudentToken { get; set; } = "";
    public string SchoolAdminToken { get; set; } = "";
    public string GlobalAdminToken { get; set; } = "";
    public Guid SchoolId { get; set; }
    public Guid IndividualId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
    public Guid SchoolAdminId { get; set; }
    public Guid GlobalAdminId { get; set; }
    public Guid ClassId { get; set; }
    public Guid LockedSimulationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid FeedbackId { get; set; }
    public Guid OrderId { get; set; }
}

sealed record TestResult(
    int Number,
    string Method,
    string Endpoint,
    int ExpectedStatus,
    int ActualStatus,
    string ContentType,
    string BodySummary,
    string Body,
    bool Passed);
