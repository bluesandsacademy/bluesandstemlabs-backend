using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using BlueSandsLMS.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using BlueSandsLMS.Infrastructure.Email;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using BlueSandsLMS.Api.Auth;
using System.Security.Claims;
using BlueSandsLMS.Application.Services.Teacher;
using BlueSandsLMS.Common.Interfaces.Dashboard;
using BlueSandsLMS.Application.Services.Dashboard;
using BlueSandsLMS.Application.Services.Student;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Application.Services.Infrastructure;
using BlueSandsLMS.Application.Services.Cache;
using ISchoolAdminAnalytics = BlueSandsLMS.Common.Interfaces.Dashboard.ISchoolAdminService;
using BlueSandsLMS.Common.Interfaces.Teacher;

var builder = WebApplication.CreateBuilder(args);

//
// ------------------------- DATABASE -------------------------
//
builder.Services.AddDbContext<BlueSandsLMSDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(cs, sql =>
    {
       sql.EnableRetryOnFailure(
    maxRetryCount: 5,
    maxRetryDelay: TimeSpan.FromSeconds(10),
    errorNumbersToAdd: null
);

    });
});

//
// ------------------------- CORE SERVICES -------------------------
//
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Infra + Core
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IParentReportService, ParentReportService>();
builder.Services.AddScoped<ITeacherAnalyticsService, TeacherAnalyticsService>();
builder.Services.AddScoped<ITeacherCommAnalyticsService, TeacherCommAnalyticsService>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<IParentLinkRepository, ParentLinkRepository>();
builder.Services.AddScoped<ICacheBustService, CacheBustService>();
builder.Services.AddScoped<ICacheInvalidator, CacheInvalidator>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISchoolAdminAnalytics, SchoolAdminService>();

// Student
builder.Services.AddScoped<IStudentDashboardService, StudentDashboardService>();
builder.Services.AddScoped<IStudentActionsService, StudentActionsService>();
builder.Services.AddScoped<IBadgeEngine, BadgeEngine>();
builder.Services.AddScoped<IStudentContentService, StudentContentService>();
builder.Services.AddScoped<IStudentLeaderboardService, StudentLeaderboardService>();

//
// ------------------------- AUTHORIZATION -------------------------
//
builder.Services.AddScoped<IAuthorizationHandler, PaidSubscriberHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PaidSubscriber", p => p.Requirements.Add(new PaidSubscriberRequirement()));
});

//
// ------------------------- AUTHENTICATION (JWT) -------------------------
//
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]
                    ?? throw new InvalidOperationException("Missing Jwt:Secret")))
        };

        // Normalize "sub" -> ClaimTypes.NameIdentifier to prevent null refs
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var id = ctx.Principal?.FindFirst("sub")?.Value;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var identity = (ClaimsIdentity)ctx.Principal!.Identity!;
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));
                }
                return Task.CompletedTask;
            }
        };
    });

//
// ------------------------- CORS -------------------------
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                // ✅ Active production deployment
                "https://bluesandsstem-001-site1.ktempurl.com",
                // ✅ New official frontend domain (if live)
                "https://app.bluesandstemlabs.com",
                // ✅ Local development
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:5173",
                "http://127.0.0.1:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//
// ------------------------- CONTROLLERS + JSON -------------------------
//
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

//
// ------------------------- SWAGGER -------------------------
//
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BlueSandsLMS API", Version = "v1" });

    // JWT security
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT like: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });

    c.CustomSchemaIds(t => t.FullName);
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.SupportNonNullableReferenceTypes();

    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
});

//
// ------------------------- APP PIPELINE -------------------------
//
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger (enabled globally)
app.UseSwagger();
app.UseSwaggerUI(s =>
{
    s.SwaggerEndpoint("/swagger/v1/swagger.json", "BlueSandsLMS.Api v1");
    s.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseCors("AllowFrontend");      // ✅ Must come before auth
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
