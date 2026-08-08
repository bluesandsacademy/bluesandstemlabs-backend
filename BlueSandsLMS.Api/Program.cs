using BlueSandsLMS.Api.Auth;
using BlueSandsLMS.Api.Infrastructure;
using BlueSandsLMS.Api.OpenApi;
using BlueSandsLMS.Api.Realtime;
using BlueSandsLMS.Api.Services;
using BlueSandsLMS.Application.Services;
using BlueSandsLMS.Application.Services.Admin;
using BlueSandsLMS.Application.Services.Cache;
using BlueSandsLMS.Application.Services.Dashboard;
using BlueSandsLMS.Application.Services.Infrastructure;
using BlueSandsLMS.Application.Services.Student;
using BlueSandsLMS.Application.Services.Teacher;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Common.Interfaces.Admin;
using BlueSandsLMS.Common.Interfaces.Student;
using BlueSandsLMS.Common.Interfaces.Teacher;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using BlueSandsLMS.Infrastructure.Email;
using BlueSandsLMS.Infrastructure.Repositories;
using BlueSandsLMS.Infrastructure.Setup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ISchoolAdminAnalytics = BlueSandsLMS.Common.Interfaces.Dashboard.ISchoolAdminService;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

ExcelPackage.License.SetNonCommercialOrganization("Blue Sands STEM Labs");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BlueSandsLMSDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(cs, sql =>
    {
        sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        sql.CommandTimeout(60);
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("monitoring-uptime");
builder.Services.AddHealthChecks().AddDbContextCheck<BlueSandsLMSDbContext>("database");

builder.Services.AddScoped<IPhETImportService, PhETImportService>();
builder.Services.AddScoped<IPhETSeedDataService, PhETSeedDataService>();

builder.Services.AddSingleton<BlueSandsLMS.Common.Interfaces.IPraxiLabsService,
    BlueSandsLMS.Application.Services.PraxiLabsService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISchoolService, SchoolService>();
builder.Services.AddScoped<IExcelUploadService, ExcelUploadService>();

//builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IEmailService, MailKitEmailService>();

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
builder.Services.AddScoped<BlueSandsLMS.Common.Interfaces.ISchoolAdminService, SchoolAdminService>();
builder.Services.AddScoped<BlueSandsLMS.Common.Interfaces.Admin.IGlobalAdminService, BlueSandsLMS.Application.Services.Admin.GlobalAdminService>();
builder.Services.AddScoped<BlueSandsLMS.Common.Interfaces.IExtendedLeaderboardService, BlueSandsLMS.Application.Services.LeaderboardService>();

builder.Services.AddScoped<IStudentDashboardService, StudentDashboardService>();
builder.Services.AddScoped<IStudentActionsService, StudentActionsService>();
builder.Services.AddScoped<IBadgeEngine, BadgeEngine>();
builder.Services.AddScoped<IStudentContentService, StudentContentService>();
builder.Services.AddScoped<IStudentLeaderboardService, StudentLeaderboardService>();
builder.Services.AddScoped<IGlobalTicketService, GlobalTicketService>();
builder.Services.AddScoped<IGlobalInsightsService, GlobalInsightsService>();
builder.Services.AddScoped<IGlobalGeoService, GlobalGeoService>();
builder.Services.AddSingleton<SessionSimulationSocketStore>();
builder.Services.AddHostedService<AssessmentAutoSubmissionHostedService>();
builder.Services.AddHostedService<MonitoringAlertsHostedService>();
builder.Services.AddHostedService<CriticalEndpointUptimeHostedService>();
builder.Services.AddHostedService<TrialExpiryHostedService>();
builder.Services.AddSingleton<PerIpRateLimitService>();
builder.Services.AddSingleton<IPlainTextInputGuard, PlainTextInputGuard>();
builder.Services.AddScoped<ApiErrorEnvelopeFilter>();
builder.Services.AddSingleton<RequestMetricsStore>();
builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetSection("Monitoring"));

builder.Services.AddScoped<IAuthorizationHandler, PaidSubscriberHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PaidSubscriber", p => p.Requirements.Add(new PaidSubscriberRequirement()));
});

// Register HttpContextAccessor and CurrentUser wrapper
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BlueSandsLMS.Api.Services.ICurrentUser, BlueSandsLMS.Api.Services.CurrentUser>();

// Resolve JWT secret — support "REPLACE_WITH_ENV_VAR: <ENVNAME>" placeholder used in appsettings
string ResolveJwtSecret(string configured)
{
    if (string.IsNullOrWhiteSpace(configured)) return configured ?? string.Empty;
    var marker = "REPLACE_WITH_ENV_VAR:";
    if (configured.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
    {
        var envKey = configured.Substring(marker.Length).Trim();
        var envVal = Environment.GetEnvironmentVariable(envKey);
        if (string.IsNullOrWhiteSpace(envVal))
            throw new InvalidOperationException($"JWT secret configuration requires environment variable '{envKey}' to be set.");
        return envVal;
    }
    return configured;
}

var configuredSecret = builder.Configuration["Jwt:Secret"];
var jwtSecret = ResolveJwtSecret(configuredSecret);

// Authentication configuration
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // CRITICAL: In .NET 9, JwtBearerHandler uses JsonWebTokenHandler internally.
        // This prevents it from remapping "sub" -> ClaimTypes.NameIdentifier, "role" -> ClaimTypes.Role, etc.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret ?? throw new InvalidOperationException("Missing Jwt:Secret"))),

            NameClaimType = "sub",
            RoleClaimType = "role"
        };

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
            },
            OnChallenge = async ctx =>
            {
                if (ctx.Response.HasStarted)
                    return;

                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var payload = ApiErrorFactory.Create(StatusCodes.Status401Unauthorized);
                ApiErrorFactory.Stamp(ctx.HttpContext, payload.Code, payload.Message);
                await ctx.Response.WriteAsJsonAsync(payload);
            },
            OnForbidden = async ctx =>
            {
                if (ctx.Response.HasStarted)
                    return;

                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                var payload = ApiErrorFactory.Create(StatusCodes.Status403Forbidden);
                ApiErrorFactory.Stamp(ctx.HttpContext, payload.Code, payload.Message);
                await ctx.Response.WriteAsJsonAsync(payload);
            }
        };
    });

const string CorsPolicy = "AllowFrontend";
string[] allowedOrigins =
{
    "https://app.bluesandstemlabs.com",
    "https://www.bluesandstemlabs.com",
    "http://localhost:3000", "http://127.0.0.1:3000",
    "http://localhost:5173", "http://127.0.0.1:5173"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                var o = origin.Trim().ToLowerInvariant();
                return allowedOrigins.Contains(o);
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("WWW-Authenticate", "Content-Disposition")
            .SetPreflightMaxAge(TimeSpan.FromHours(24));
    });
});

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
})
.AddMvcOptions(options =>
{
    options.Filters.Add<ApiErrorEnvelopeFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Blue Sands STEM Labs API",
        Version = "v1",
        Description = "Official API for Blue Sands STEM Labs virtual science learning workflows, including ILS authoring, student session flow, simulation runtime, moderation, scoring, and teacher analytics.",
        Contact = new OpenApiContact
        {
            Name = "Blue Sands STEM Labs",
            Email = "support@bluesandstemlabs.com",
            Url = new Uri("https://www.bluesandstemlabs.com")
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter token only (no \"Bearer \" prefix) if Swagger UI shows the token field."
    });

    // Ensure operations that use [Authorize] receive the security requirement
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            },
            Scheme = "bearer",
            Name = "Bearer",
            In = ParameterLocation.Header
        },
            Array.Empty<string>()
        }
    });

    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    c.SupportNonNullableReferenceTypes();

    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });

    c.DescribeAllParametersInCamelCase();
    c.OperationFilter<FrontendOpenApiOperationFilter>();
    c.SchemaFilter<EnumMetadataSchemaFilter>();

    c.IncludeXmlCommentsFromAssemblies(
        typeof(Program).Assembly,
        typeof(User).Assembly,
        typeof(AuthService).Assembly,
        typeof(IAuthService).Assembly,
        typeof(BlueSandsLMSDbContext).Assembly);

    c.AddServer(new OpenApiServer { Url = "/", Description = "Current host" });
    c.AddServer(new OpenApiServer { Url = "http://localhost:5245", Description = "Local development" });

    c.CustomSchemaIds(t => t.FullName);
});

var app = builder.Build();

if (args.Any(a => string.Equals(a, "--seed-uat", StringComparison.OrdinalIgnoreCase)))
{
    await using var seedScope = app.Services.CreateAsyncScope();
    var db = seedScope.ServiceProvider.GetRequiredService<BlueSandsLMSDbContext>();
    var loggerFactory = seedScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("UatSeed");

    await db.Database.MigrateAsync();
    await UatSeedDataService.SeedAsync(db, logger);
    return;
}

if (builder.Configuration.GetValue<bool>("Seeding:RunUatSeedOnStartup"))
{
    await using var seedScope = app.Services.CreateAsyncScope();
    var db = seedScope.ServiceProvider.GetRequiredService<BlueSandsLMSDbContext>();
    var loggerFactory = seedScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("UatSeed");
    await db.Database.MigrateAsync();
    await UatSeedDataService.SeedAsync(db, logger);
}

app.UseSwagger();
app.UseSwaggerUI(s =>
{
    s.SwaggerEndpoint("/swagger/v1/swagger.json", "Blue Sands STEM Labs API v1");
    s.RoutePrefix = "swagger";
    s.DocumentTitle = "Blue Sands STEM Labs API Docs";
    s.InjectStylesheet("/swagger-ui/custom.css");
    s.InjectJavascript("/swagger-ui/custom.js");
    s.DisplayRequestDuration();
    s.DefaultModelsExpandDepth(-1);
    s.EnablePersistAuthorization();
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async ctx =>
    {
        ctx.Response.ContentType = "application/json";
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var ex = feature?.Error;

        var status = ex switch
        {
            ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        ctx.Response.StatusCode = status;
        var message = status == StatusCodes.Status500InternalServerError
            ? ApiErrorFactory.DefaultMessageForStatus(status)
            : ex?.Message;

        var payload = ApiErrorFactory.Create(status, message: message);
        ApiErrorFactory.Stamp(ctx, payload.Code, payload.Message);
        await ctx.Response.WriteAsJsonAsync(payload);
    });
});

app.UseStatusCodePages(async statusContext =>
{
    var ctx = statusContext.HttpContext;
    if (ctx.Response.HasStarted)
        return;

    var status = ctx.Response.StatusCode;
    if (status < 400)
        return;

    var payload = ApiErrorFactory.Create(status);
    ApiErrorFactory.Stamp(ctx, payload.Code, payload.Message);
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(payload);
});

app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseMiddleware<RequestAuditLoggingMiddleware>();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.Use(async (ctx, next) =>
{
    var origin = ctx.Request.Headers["Origin"].ToString();
    if (!string.IsNullOrEmpty(origin) &&
        allowedOrigins.Contains(origin.Trim().ToLowerInvariant()))
    {

        ctx.Response.Headers["Vary"] = "Origin";

        if (HttpMethods.IsOptions(ctx.Request.Method))
        {

            ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
            ctx.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,PATCH,DELETE,OPTIONS";
            var reqHdrs = ctx.Request.Headers["Access-Control-Request-Headers"].ToString();
            ctx.Response.Headers["Access-Control-Allow-Headers"] =
                string.IsNullOrWhiteSpace(reqHdrs)
                    ? "Authorization, Content-Type, Accept, X-Requested-With"
                    : reqHdrs;
            ctx.Response.Headers["Access-Control-Expose-Headers"] = "WWW-Authenticate, Content-Disposition";
            ctx.Response.Headers["Access-Control-Max-Age"] = "86400";
            ctx.Response.StatusCode = 204;
            return;
        }

        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
            ctx.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            ctx.Response.Headers["Access-Control-Expose-Headers"] = "WWW-Authenticate, Content-Disposition";
            return Task.CompletedTask;
        });
    }

    await next();
});

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseMiddleware<PostRateLimitMiddleware>();
app.UseAuthorization();

app.MapControllers().RequireCors(CorsPolicy);
app.MapSessionSimulationSocketEndpoint();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();