using System.Linq;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreAssignmentType = BlueSandsLMS.Core.Entities.AssignmentType;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/ils")]
    [Authorize]
    public class IlsController : ControllerBase
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly ILogger<IlsController> _logger;
        private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

        public IlsController(BlueSandsLMSDbContext db, ILogger<IlsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Teacher,Student,GlobalAdmin")]
        public async Task<IActionResult> List([FromQuery] Guid? classId, CancellationToken ct)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            if (classId.HasValue)
            {
                var hasClassAccess = await _db.Enrollments
                    .AnyAsync(
                        e => e.ClassroomId == classId.Value &&
                             e.UserId == userId &&
                             (e.RoleInClass == ClassRole.Teacher || e.RoleInClass == ClassRole.Student),
                        ct);
                if (!hasClassAccess)
                    return Forbid();

                var ilsIds = await GetIlsIdsForClassAsync(classId.Value, ct);
                if (ilsIds.Count == 0)
                    return Ok(Array.Empty<IlsDetailsDto>());

                var ilsList = await _db.InteractiveLearningSpaces
                    .Where(i => ilsIds.Contains(i.Id) && i.Status == IlsStatus.Published)
                    .Include(i => i.Tags).ThenInclude(t => t.Tag)
                    .ToListAsync(ct);

                return Ok(ilsList.Select(Map));
            }

            if (User.IsInRole("Teacher"))
            {
                var mine = await _db.InteractiveLearningSpaces
                    .Where(i => i.CreatedBy == userId)
                    .Include(i => i.Tags).ThenInclude(t => t.Tag)
                    .ToListAsync(ct);

                var teacherSchoolId = await _db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.SchoolId)
                    .FirstOrDefaultAsync(ct);

                var shared = new List<InteractiveLearningSpace>();
                if (teacherSchoolId.HasValue)
                {
                    shared = await _db.InteractiveLearningSpaces
                        .Where(i => i.SchoolId == teacherSchoolId && i.IsSharedWithSchool && i.CreatedBy != userId)
                        .Include(i => i.Tags).ThenInclude(t => t.Tag)
                        .ToListAsync(ct);
                }

                var combined = mine.Concat(shared).OrderByDescending(i => i.UpdatedAt).ToList();
                return Ok(combined.Select(Map));
            }

            var studentIlsIds = await GetIlsIdsForStudentAsync(userId, ct);
            if (studentIlsIds.Count == 0)
                return Ok(Array.Empty<IlsDetailsDto>());

            var assigned = await _db.InteractiveLearningSpaces
                .Where(i => studentIlsIds.Contains(i.Id) && i.Status == IlsStatus.Published)
                .Include(i => i.Tags).ThenInclude(t => t.Tag)
                .ToListAsync(ct);

            return Ok(assigned.Select(Map));
        }

        [HttpGet("by-teacher/{teacherId:guid}")]
        [Authorize(Roles = "SchoolAdmin,SuperAdmin,GlobalAdmin")]
        public async Task<IActionResult> GetByTeacherId(Guid teacherId, CancellationToken ct)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var teacher = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == teacherId, ct);

            if (teacher == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Teacher not found.");

            var teacherRoleName = teacher.Role?.Name
                ?? await _db.Roles.Where(r => r.Id == teacher.RoleId).Select(r => r.Name).FirstOrDefaultAsync(ct);

            if (!string.Equals(teacherRoleName, "Teacher", StringComparison.OrdinalIgnoreCase))
                return Error(StatusCodes.Status400BadRequest, "INVALID_ROLE", "The specified user is not a teacher.");

            var ilsList = await _db.InteractiveLearningSpaces
                .Where(i => i.CreatedBy == teacherId)
                .Include(i => i.Tags).ThenInclude(t => t.Tag)
                .ToListAsync(ct);

            return Ok(ilsList.Select(Map));
        }

        [HttpPost]
        [Authorize(Roles = "Teacher,GlobalAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateIlsRequest request, CancellationToken ct)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var simulationSource = (request.SimulationSource ?? "phet").Trim().ToLowerInvariant();
            if (simulationSource != "phet" && simulationSource != "praxilabs")
                simulationSource = "phet";

            var details = new List<(string field, string issue)>();
            if (string.IsNullOrWhiteSpace(request.Title)) details.Add(("title", "Title is required"));
            if (string.IsNullOrWhiteSpace(request.Objective)) details.Add(("objective", "Objective is required"));
            if (!TryParseDurationToMinutes(request.Duration, out var durationMinutes))
                details.Add(("duration", "Duration must be a positive time (e.g. '2 hours' or '30 minutes')"));
            if (request.Tags == null || request.Tags.Count == 0) details.Add(("tags", "At least one tag is required"));
            if (request.PreSimAssessment?.Questions == null || request.PreSimAssessment.Questions.Count == 0)
                details.Add(("preSimAssessment.questions", "At least one pre-simulation question is required"));
            if (request.PostSimAssessment?.Questions == null || request.PostSimAssessment.Questions.Count == 0)
                details.Add(("postSimAssessment.questions", "At least one post-simulation question is required"));

            Guid? simulationId = null;
            string? praxiLabsExperimentId = null;

            if (simulationSource == "phet")
            {
                if (!Guid.TryParse(request.SimulationId, out var parsedSimId) || parsedSimId == Guid.Empty)
                    details.Add(("simulationId", "SimulationId must be a valid GUID for PhET simulations"));
                else
                    simulationId = parsedSimId;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.PraxiLabsExperimentId))
                    details.Add(("praxiLabsExperimentId", "PraxiLabsExperimentId is required for PraxiLabs experiments"));
                else
                    praxiLabsExperimentId = request.PraxiLabsExperimentId.Trim();
            }

            if (details.Count > 0)
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", details.ToArray());

            if (simulationSource == "phet" && simulationId.HasValue)
            {
                var simulationExists = await _db.PhETSimulations
                    .AnyAsync(s => s.Id == simulationId.Value && s.IsActive, ct);
                if (!simulationExists)
                    return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("simulationId", "Simulation not found"));
            }

            var rawTags = (request.Tags ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (rawTags.Count == 0)
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("tags", "At least one tag is required"));

            var tagIdsFromPayload = new HashSet<Guid>();
            var tagLabelsFromPayload = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in rawTags)
            {
                if (Guid.TryParse(value, out var tagId))
                {
                    tagIdsFromPayload.Add(tagId);
                }
                else
                {
                    tagLabelsFromPayload.Add(value);
                }
            }

            var resolvedTagIds = new HashSet<Guid>();
            if (tagIdsFromPayload.Count > 0)
            {
                var existingTagIds = await _db.CurriculumTags
                    .Where(t => tagIdsFromPayload.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync(ct);
                foreach (var tagId in existingTagIds)
                    resolvedTagIds.Add(tagId);

                if (existingTagIds.Count != tagIdsFromPayload.Count)
                    return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("tags", "One or more tag IDs are invalid"));
            }

            if (tagLabelsFromPayload.Count > 0)
            {
                var normalizedLabels = tagLabelsFromPayload
                    .Select(x => x.ToLowerInvariant())
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                var existingByLabel = await _db.CurriculumTags
                    .Where(t => normalizedLabels.Contains(t.Label.ToLower()))
                    .Select(t => new { t.Id, t.Label })
                    .ToListAsync(ct);

                foreach (var row in existingByLabel)
                    resolvedTagIds.Add(row.Id);

                var matchedLabels = existingByLabel
                    .Select(x => x.Label.Trim().ToLowerInvariant())
                    .ToHashSet(StringComparer.Ordinal);
                var missingLabels = normalizedLabels
                    .Where(label => !matchedLabels.Contains(label))
                    .ToList();

                if (missingLabels.Count > 0)
                {
                    foreach (var missingLabel in missingLabels)
                    {

                        var originalLabel = tagLabelsFromPayload
                            .FirstOrDefault(l => l.Equals(missingLabel, StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrWhiteSpace(originalLabel))
                        {
                            var newTag = new CurriculumTag
                            {
                                Id = Guid.NewGuid(),
                                Label = originalLabel.Trim(),
                                Subject = "General",
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.CurriculumTags.Add(newTag);
                            resolvedTagIds.Add(newTag.Id);
                        }
                    }
                    await _db.SaveChangesAsync(ct);
                }
            }

            var requestedTagIds = resolvedTagIds.ToList();
            if (requestedTagIds.Count == 0)
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("tags", "At least one valid tag is required"));

            var creatorSchoolId = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(ct);

            var now = DateTime.UtcNow;
            var trimmedScore = request.Score?.Trim() ?? string.Empty;
            var preSimAssessment = request.PreSimAssessment ?? new QuizData();
            var postSimAssessment = request.PostSimAssessment ?? new QuizData();
            var pollOptions = preSimAssessment.Questions.FirstOrDefault()?.Options?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
            if (pollOptions.Count == 0)
            {
                pollOptions = new List<string> { "Strongly Disagree", "Disagree", "Agree", "Strongly Agree" };
            }

            var questionPoints = TryParsePositiveDecimal(postSimAssessment.Points, out var points)
                ? points
                : 1m;
            var postQuestions = postSimAssessment.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.Question))
                .Select((q, index) => new
                {
                    qId = $"q{index + 1}",
                    type = q.Options != null && q.Options.Count > 0 ? "mcq" : "short",
                    points = questionPoints,
                    correctAnswer = q.CorrectAnswer,
                    expectedKeywords = q.Options != null && q.Options.Count == 0 && !string.IsNullOrWhiteSpace(q.CorrectAnswer)
                        ? new[] { q.CorrectAnswer.Trim() }
                        : Array.Empty<string>(),
                    feedback = $"Review question {index + 1} and compare with your experiment outcomes."
                })
                .ToList();

            var assessmentConfig = new
            {
                score = trimmedScore,
                duration = FormatDurationHours(durationMinutes),
                introductionMessage = request.IntroductionMessage?.Trim(),
                engagementQuestion = request.EngagementQuestion?.Trim(),
                hypothesisQuestion = request.HypothesisQuestion?.Trim(),
                experimentProcedures = (request.ExperimentProcedures ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList(),
                discussionPrompt = request.DiscussionPrompt?.Trim(),
                realWorldApplications = (request.RealWorldApplications ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList(),
                relatedCareers = (request.RelatedCareers ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList(),
                realWorldTask = request.RealWorldTask?.Trim(),
                preSimAssessment,
                postSimAssessment,
                questions = postQuestions
            };

            var ils = new InteractiveLearningSpace
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Objective = request.Objective.Trim(),
                Grade = trimmedScore,
                DurationMinutes = durationMinutes,
                SimulationId = simulationId,
                SimulationSource = simulationSource,
                PraxiLabsExperimentId = praxiLabsExperimentId,
                PollOptionsJson = JsonSerializer.Serialize(pollOptions),
                AssessmentConfigJson = JsonSerializer.Serialize(assessmentConfig),
                Status = IlsStatus.Draft,
                CreatedBy = userId,
                SchoolId = creatorSchoolId,
                IsSharedWithSchool = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.InteractiveLearningSpaces.Add(ils);
            foreach (var tagId in requestedTagIds)
            {
                _db.IlsTags.Add(new IlsTag
                {
                    IlsId = ils.Id,
                    TagId = tagId
                });
            }

            await _db.SaveChangesAsync(ct);
            return StatusCode(StatusCodes.Status201Created, new { id = ils.Id, status = "draft" });
        }

        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Teacher,Student,GlobalAdmin")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var ils = await _db.InteractiveLearningSpaces
                .Include(x => x.Tags).ThenInclude(x => x.Tag)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (ils == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ILS not found.");

            if (User.IsInRole("GlobalAdmin"))
                return Ok(MapFormData(ils));

            if (User.IsInRole("Teacher"))
            {
                if (ils.CreatedBy == userId)
                    return Ok(MapFormData(ils));

                if (ils.IsSharedWithSchool)
                {
                    var teacherSchoolId = await _db.Users
                        .Where(u => u.Id == userId)
                        .Select(u => u.SchoolId)
                        .FirstOrDefaultAsync(ct);

                    if (teacherSchoolId.HasValue && ils.SchoolId == teacherSchoolId)
                        return Ok(MapFormData(ils));
                }

                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only access ILS resources you own or that are shared within your school.");
            }

            if (ils.Status != IlsStatus.Published)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Published ILS not found.");

            var hasStudentAccess = await StudentHasIlsAccessAsync(userId, id, ct);
            if (!hasStudentAccess)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only access ILS assigned to your classes.");

            return Ok(MapFormData(ils));
        }

[HttpPut("{id:guid}")]
[Authorize(Roles = "Teacher,GlobalAdmin")]
public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIlsRequest request, CancellationToken ct)
{
    var userId = CurrentUserId();
    if (userId == Guid.Empty)
        return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

    var ils = await _db.InteractiveLearningSpaces
        .Include(x => x.Tags)
        .FirstOrDefaultAsync(x => x.Id == id, ct);

    if (ils == null)
        return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ILS not found.");
    if (ils.CreatedBy != userId)
        return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only modify ILS resources you own.");
    if (ils.Status == IlsStatus.Published)
        return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Published ILS cannot be modified.");

    if (request.Title != null)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("title", "Title cannot be empty"));
        ils.Title = request.Title.Trim();
    }

    if (request.Objective != null)
    {
        if (string.IsNullOrWhiteSpace(request.Objective))
            return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("objective", "Objective cannot be empty"));
        ils.Objective = request.Objective.Trim();
    }

    if (request.Score != null)
    {
        if (string.IsNullOrWhiteSpace(request.Score))
            return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("score", "Score cannot be empty"));
        ils.Grade = request.Score.Trim();
    }

        if (request.Duration != null)
        {
            if (!TryParseDurationToMinutes(request.Duration, out var durationMinutes))
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("duration", "Duration must be a positive time (e.g. '2 hours' or '30 minutes')"));
            ils.DurationMinutes = durationMinutes;
        }

    if (request.SimulationSource != null || request.SimulationId != null || request.PraxiLabsExperimentId != null)
    {
        var updatedSource = (request.SimulationSource ?? ils.SimulationSource ?? "phet").Trim().ToLowerInvariant();
        if (updatedSource != "phet" && updatedSource != "praxilabs") updatedSource = "phet";

        if (updatedSource == "phet")
        {
            if (request.SimulationId != null)
            {
                if (!Guid.TryParse(request.SimulationId, out var simId) || simId == Guid.Empty)
                    return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("simulationId", "SimulationId must be a valid GUID"));

                var simulationExists = await _db.PhETSimulations
                    .AnyAsync(s => s.Id == simId && s.IsActive, ct);
                if (!simulationExists)
                    return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("simulationId", "Simulation not found"));

                ils.SimulationId = simId;
            }
            ils.PraxiLabsExperimentId = null;
        }
        else
        {
            if (request.PraxiLabsExperimentId != null)
            {
                if (string.IsNullOrWhiteSpace(request.PraxiLabsExperimentId))
                    return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("praxiLabsExperimentId", "PraxiLabsExperimentId cannot be empty"));

                ils.PraxiLabsExperimentId = request.PraxiLabsExperimentId.Trim();
            }
            ils.SimulationId = null;
        }

        ils.SimulationSource = updatedSource;
    }

    if (request.Tags != null)
    {
        var rawTags = request.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rawTags.Count == 0)
            return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("tags", "At least one tag is required"));

        var tagIdsFromPayload = new HashSet<Guid>();
        var tagLabelsFromPayload = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in rawTags)
        {
            if (Guid.TryParse(value, out var tagId))
                tagIdsFromPayload.Add(tagId);
            else
                tagLabelsFromPayload.Add(value);
        }

        var resolvedTagIds = new HashSet<Guid>();
        if (tagIdsFromPayload.Count > 0)
        {
            var existingTagIds = await _db.CurriculumTags
                .Where(t => tagIdsFromPayload.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync(ct);
            foreach (var tagId in existingTagIds)
                resolvedTagIds.Add(tagId);

            if (existingTagIds.Count != tagIdsFromPayload.Count)
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("tags", "One or more tag IDs are invalid"));
        }

        if (tagLabelsFromPayload.Count > 0)
        {
            var normalizedLabels = tagLabelsFromPayload
                .Select(x => x.ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var existingByLabel = await _db.CurriculumTags
                .Where(t => normalizedLabels.Contains(t.Label.ToLower()))
                .Select(t => new { t.Id, t.Label })
                .ToListAsync(ct);

            foreach (var row in existingByLabel)
                resolvedTagIds.Add(row.Id);

            var matchedLabels = existingByLabel
                .Select(x => x.Label.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.Ordinal);
            var missingLabels = normalizedLabels
                .Where(label => !matchedLabels.Contains(label))
                .ToList();

            if (missingLabels.Count > 0)
            {
                foreach (var missingLabel in missingLabels)
                {
                    var originalLabel = tagLabelsFromPayload
                        .FirstOrDefault(l => l.Equals(missingLabel, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(originalLabel))
                    {
                        var newTag = new CurriculumTag
                        {
                            Id = Guid.NewGuid(),
                            Label = originalLabel.Trim(),
                            Subject = "General",
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.CurriculumTags.Add(newTag);
                        resolvedTagIds.Add(newTag.Id);
                    }
                }
                await _db.SaveChangesAsync(ct);
            }
        }

        _db.IlsTags.RemoveRange(ils.Tags);
        foreach (var tagId in resolvedTagIds)
        {
            _db.IlsTags.Add(new IlsTag { IlsId = ils.Id, TagId = tagId });
        }
    }

    var hasContentUpdate =
        request.PreSimAssessment != null ||
        request.PostSimAssessment != null ||
        request.IntroductionMessage != null ||
        request.EngagementQuestion != null ||
        request.HypothesisQuestion != null ||
        request.ExperimentProcedures != null ||
        request.DiscussionPrompt != null ||
        request.RealWorldApplications != null ||
        request.RelatedCareers != null ||
        request.RealWorldTask != null ||
        request.Score != null ||
        request.Duration != null;

    if (hasContentUpdate)
    {
        var existingConfig = ParseAssessmentConfig(ils.AssessmentConfigJson) ?? new IlsAssessmentConfig();

        var preSimAssessment = request.PreSimAssessment ?? existingConfig.PreSimAssessment ?? new QuizData();
        var postSimAssessment = request.PostSimAssessment ?? existingConfig.PostSimAssessment ?? new QuizData();

        var trimmedScore = request.Score?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedScore))
            trimmedScore = existingConfig.Score?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedScore))
            trimmedScore = ils.Grade;

        var durationMinutes = ils.DurationMinutes;
        if (request.Duration != null)
        {
            if (!TryParseDurationToMinutes(request.Duration, out durationMinutes))
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", ("duration", "Duration must be a positive time (e.g. '2 hours' or '30 minutes')"));
        }
        var trimmedDuration = FormatDurationHours(durationMinutes);

        var introductionMessage = request.IntroductionMessage == null
            ? existingConfig.IntroductionMessage?.Trim()
            : request.IntroductionMessage.Trim();

        var engagementQuestion = request.EngagementQuestion == null
            ? existingConfig.EngagementQuestion?.Trim()
            : request.EngagementQuestion.Trim();

        var hypothesisQuestion = request.HypothesisQuestion == null
            ? existingConfig.HypothesisQuestion?.Trim()
            : request.HypothesisQuestion.Trim();

        var experimentProcedures = request.ExperimentProcedures != null
            ? request.ExperimentProcedures
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
            : NormalizeStrings(existingConfig.ExperimentProcedures);

        var discussionPrompt = request.DiscussionPrompt == null
            ? existingConfig.DiscussionPrompt?.Trim()
            : request.DiscussionPrompt.Trim();

        var realWorldApplications = request.RealWorldApplications != null
            ? request.RealWorldApplications
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
            : NormalizeStrings(existingConfig.RealWorldApplications);

        var relatedCareers = request.RelatedCareers != null
            ? request.RelatedCareers
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
            : NormalizeStrings(existingConfig.RelatedCareers);

        var realWorldTask = request.RealWorldTask == null
            ? existingConfig.RealWorldTask?.Trim()
            : request.RealWorldTask.Trim();

        var pollOptions = preSimAssessment.Questions?.FirstOrDefault()?.Options?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();
        if (pollOptions.Count == 0)
            pollOptions = new List<string> { "Strongly Disagree", "Disagree", "Agree", "Strongly Agree" };

        var questionPoints = TryParsePositiveDecimal(postSimAssessment.Points, out var points) ? points : 1m;
        var postQuestions = (postSimAssessment.Questions ?? new List<QuizQuestion>())
            .Where(q => !string.IsNullOrWhiteSpace(q.Question))
            .Select((q, index) => new
            {
                qId = $"q{index + 1}",
                type = q.Options != null && q.Options.Count > 0 ? "mcq" : "short",
                points = questionPoints,
                correctAnswer = q.CorrectAnswer,
                expectedKeywords = q.Options != null && q.Options.Count == 0 && !string.IsNullOrWhiteSpace(q.CorrectAnswer)
                    ? new[] { q.CorrectAnswer.Trim() }
                    : Array.Empty<string>(),
                feedback = $"Review question {index + 1} and compare with your experiment outcomes."
            })
            .ToList();

        var assessmentConfig = new
        {
            score = trimmedScore,
            duration = trimmedDuration,
            introductionMessage = introductionMessage,
            engagementQuestion = engagementQuestion,
            hypothesisQuestion = hypothesisQuestion,
            experimentProcedures,
            discussionPrompt,
            realWorldApplications,
            relatedCareers,
            realWorldTask,
            preSimAssessment,
            postSimAssessment,
            questions = postQuestions
        };

        ils.AssessmentConfigJson = JsonSerializer.Serialize(assessmentConfig);
        ils.PollOptionsJson = JsonSerializer.Serialize(pollOptions);
    }

    ils.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync(ct);

    var updated = await _db.InteractiveLearningSpaces
        .Include(x => x.Tags).ThenInclude(x => x.Tag)
        .FirstAsync(x => x.Id == id, ct);

    return Ok(new { id = updated.Id });
}

        [HttpPost("{id:guid}/assign")]
        [Authorize(Roles = "Teacher,GlobalAdmin")]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignIlsRequest request, CancellationToken ct)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var details = new List<(string field, string issue)>();
            if (request.ClassroomId == Guid.Empty)
                details.Add(("classroomId", "ClassroomId is required"));

            if (!TryMapAssignmentType(request.Type, out var assignmentType, out var normalizedType))
                details.Add(("type", "Type must be one of: classwork, assignment, test, exam"));

            if (details.Count > 0)
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Validation failed", details.ToArray());

            var ils = await _db.InteractiveLearningSpaces
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (ils == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ILS not found.");

            if (ils.CreatedBy != userId)
            {
                if (!ils.IsSharedWithSchool)
                    return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only assign ILS resources you own.");

                var teacherSchoolId = await _db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.SchoolId)
                    .FirstOrDefaultAsync(ct);

                if (!teacherSchoolId.HasValue || ils.SchoolId != teacherSchoolId)
                    return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only assign ILS resources shared within your school.");
            }

            if (ils.Status != IlsStatus.Published)
                return Error(StatusCodes.Status422UnprocessableEntity, "BUSINESS_RULE_ERROR", "Only published ILS can be assigned to classes.");

            var isTeacherOfClass = await _db.Enrollments
                .AnyAsync(
                    e => e.ClassroomId == request.ClassroomId &&
                         e.UserId == userId &&
                         e.RoleInClass == ClassRole.Teacher,
                    ct);
            if (!isTeacherOfClass)
                return Forbid();

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                ClassroomId = request.ClassroomId,
                Title = ils.Title,
                Type = assignmentType,
                ResourceCode = $"ils:{ils.Id:D}",
                DueAt = null,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Assignments.Add(assignment);
            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                id = assignment.Id,
                ilsId = ils.Id,
                classroomId = assignment.ClassroomId,
                type = normalizedType,
                resourceCode = assignment.ResourceCode
            });
        }

        [HttpPost("{id:guid}/share")]
        [Authorize(Roles = "Teacher,GlobalAdmin")]
        public async Task<IActionResult> Share(Guid id, CancellationToken ct)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var ils = await _db.InteractiveLearningSpaces
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (ils == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ILS not found.");
            if (ils.CreatedBy != userId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Only the owner can share this ILS.");
            if (ils.SchoolId == null)
                return Error(StatusCodes.Status400BadRequest, "BUSINESS_RULE_ERROR", "ILS has no associated school and cannot be shared.");

            ils.IsSharedWithSchool = true;
            ils.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { shared = true });
        }

        [HttpDelete("{id:guid}/share")]
        [Authorize(Roles = "Teacher,GlobalAdmin")]
        public async Task<IActionResult> Unshare(Guid id, CancellationToken ct)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var ils = await _db.InteractiveLearningSpaces
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (ils == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ILS not found.");
            if (ils.CreatedBy != userId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Only the owner can unshare this ILS.");

            ils.IsSharedWithSchool = false;
            ils.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { shared = false });
        }

        [HttpPost("{id:guid}/publish")]
        [Authorize(Roles = "Teacher,GlobalAdmin")]
        public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");

            var ils = await _db.InteractiveLearningSpaces
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (ils == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "ILS not found.");
            if (ils.CreatedBy != userId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only publish ILS resources you own.");
            if (ils.Status == IlsStatus.Published)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "ILS is already published.");

            var details = new List<(string field, string issue)>();
            if (string.IsNullOrWhiteSpace(ils.Title)) details.Add(("title", "Title is required"));
            if (string.IsNullOrWhiteSpace(ils.Objective)) details.Add(("objective", "Objective is required"));
            if (ils.DurationMinutes <= 0) details.Add(("duration", "Duration must be greater than zero"));
            if (ils.Tags.Count == 0) details.Add(("tags", "At least one tag is required before publishing"));

            var source = (ils.SimulationSource ?? "phet").Trim().ToLowerInvariant();
            if (source == "phet")
            {
                if (!ils.SimulationId.HasValue || ils.SimulationId == Guid.Empty)
                    details.Add(("simulationId", "A valid PhET simulation is required before publishing"));
                else
                {
                    var simulationExists = await _db.PhETSimulations.AnyAsync(s => s.Id == ils.SimulationId.Value && s.IsActive, ct);
                    if (!simulationExists) details.Add(("simulationId", "A valid simulation is required before publishing"));
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ils.PraxiLabsExperimentId))
                    details.Add(("praxiLabsExperimentId", "A PraxiLabs experiment ID is required before publishing"));
            }

            if (details.Count > 0)
                return Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "ILS is incomplete and cannot be published.", details.ToArray());

            ils.Status = IlsStatus.Published;
            var now = DateTime.UtcNow;
            ils.UpdatedAt = now;

            _db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                Utc = now,
                ActorUserId = userId,
                Category = "ils",
                Name = "Ils.Published",
                ExtraJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ilsId = ils.Id,
                    teacherId = userId,
                    timestamp = now
                })
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "ILS published: ilsId={IlsId}, teacherId={TeacherId}, at={Timestamp}",
                ils.Id,
                userId,
                now);

            return Ok(new { status = "published", url = $"/api/ils/{ils.Id}" });
        }

        private async Task<HashSet<Guid>> GetIlsIdsForClassAsync(Guid classId, CancellationToken ct)
        {
            var ilsIds = new HashSet<Guid>();

            var assignmentCodes = await _db.Assignments
                .AsNoTracking()
                .Where(a => a.ClassroomId == classId && a.ResourceCode != null)
                .Select(a => a.ResourceCode!)
                .ToListAsync(ct);

            foreach (var assignmentCode in assignmentCodes)
            {
                var assignedIlsId = ParseIlsResourceCode(assignmentCode);
                if (assignedIlsId.HasValue)
                    ilsIds.Add(assignedIlsId.Value);
            }

            var sessionIlsIds = await (from s in _db.StudentIlsSessions
                                       join e in _db.Enrollments on s.StudentId equals e.UserId
                                       where e.ClassroomId == classId
                                       select s.IlsId)
                                      .Distinct()
                                      .ToListAsync(ct);

            ilsIds.UnionWith(sessionIlsIds);
            return ilsIds;
        }

        private async Task<HashSet<Guid>> GetIlsIdsForStudentAsync(Guid studentId, CancellationToken ct)
        {
            var ilsIds = new HashSet<Guid>();

            var classroomIds = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == studentId && e.RoleInClass == ClassRole.Student)
                .Select(e => e.ClassroomId)
                .Distinct()
                .ToListAsync(ct);

            if (classroomIds.Count > 0)
            {
                var assignmentCodes = await _db.Assignments
                    .AsNoTracking()
                    .Where(a => classroomIds.Contains(a.ClassroomId) && a.ResourceCode != null)
                    .Select(a => a.ResourceCode!)
                    .ToListAsync(ct);

                foreach (var assignmentCode in assignmentCodes)
                {
                    var assignedIlsId = ParseIlsResourceCode(assignmentCode);
                    if (assignedIlsId.HasValue)
                        ilsIds.Add(assignedIlsId.Value);
                }
            }

            var sessionIlsIds = await _db.StudentIlsSessions
                .AsNoTracking()
                .Where(x => x.StudentId == studentId)
                .Select(x => x.IlsId)
                .Distinct()
                .ToListAsync(ct);

            ilsIds.UnionWith(sessionIlsIds);
            return ilsIds;
        }

        private async Task<bool> StudentHasIlsAccessAsync(Guid studentId, Guid ilsId, CancellationToken ct)
        {
            var hasSession = await _db.StudentIlsSessions
                .AsNoTracking()
                .AnyAsync(x => x.StudentId == studentId && x.IlsId == ilsId, ct);

            if (hasSession)
                return true;

            var studentIlsIds = await GetIlsIdsForStudentAsync(studentId, ct);
            return studentIlsIds.Contains(ilsId);
        }

        private static bool TryParsePositiveDecimal(string? raw, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (!decimal.TryParse(raw.Trim(), out var parsed))
                return false;

            if (parsed <= 0m)
                return false;

            value = parsed;
            return true;
        }

        private static Guid? ParseIlsResourceCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var value = raw.Trim();
            if (value.StartsWith("ils:", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(4);

            return Guid.TryParse(value, out var ilsId) ? ilsId : null;
        }

        private static bool TryMapAssignmentType(string? raw, out CoreAssignmentType assignmentType, out string normalizedType)
        {
            normalizedType = raw?.Trim().ToLowerInvariant() ?? string.Empty;
            assignmentType = CoreAssignmentType.Experiment;

            switch (normalizedType)
            {
                case "classwork":
                    assignmentType = CoreAssignmentType.Lesson;
                    return true;
                case "assignment":
                    assignmentType = CoreAssignmentType.Experiment;
                    return true;
                case "test":
                    assignmentType = CoreAssignmentType.Test;
                    return true;
                case "exam":
                    assignmentType = CoreAssignmentType.Exam;
                    return true;
                default:
                    return false;
            }
        }

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
        }

        private static IlsDetailsDto Map(InteractiveLearningSpace ils) =>
            new()
            {
                Id = ils.Id,
                Title = ils.Title,
                Objective = ils.Objective,
                Grade = ils.Grade,
                Duration = FormatDurationHours(ils.DurationMinutes),
                SimulationId = ils.SimulationId,
                SimulationSource = ils.SimulationSource ?? "phet",
                PraxiLabsExperimentId = ils.PraxiLabsExperimentId,
                Status = ils.Status == IlsStatus.Published ? "published" : "draft",
                CreatedBy = ils.CreatedBy,
                IsSharedWithSchool = ils.IsSharedWithSchool,
                CreatedAt = ils.CreatedAt,
                UpdatedAt = ils.UpdatedAt,
                Tags = ils.Tags
                    .Where(x => x.Tag != null)
                    .Select(x => new IlsTagItemDto
                    {
                        Id = x.TagId,
                        Label = x.Tag!.Label,
                        Subject = x.Tag.Subject
                    })
                    .ToList()
            };

        private static IlsFormDataDto MapFormData(InteractiveLearningSpace ils)
        {
            var config = ParseAssessmentConfig(ils.AssessmentConfigJson);
            var score = config?.Score?.Trim();

            return new IlsFormDataDto
            {
                Id = ils.Id,
                Title = ils.Title,
                Objective = ils.Objective,
                Score = string.IsNullOrWhiteSpace(score) ? ils.Grade : score,
                Duration = FormatDurationHours(ils.DurationMinutes),
                SimulationId = ils.SimulationId?.ToString() ?? string.Empty,
                SimulationSource = ils.SimulationSource ?? "phet",
                PraxiLabsExperimentId = ils.PraxiLabsExperimentId,
                PreSimAssessment = config?.PreSimAssessment ?? new QuizData(),
                PostSimAssessment = config?.PostSimAssessment ?? new QuizData(),
                Tags = ils.Tags
                    .Where(x => x.Tag != null)
                    .Select(x => x.Tag!.Label)
                    .ToList(),
                IntroductionMessage = config?.IntroductionMessage?.Trim() ?? string.Empty,
                EngagementQuestion = config?.EngagementQuestion?.Trim() ?? string.Empty,
                HypothesisQuestion = config?.HypothesisQuestion?.Trim() ?? string.Empty,
                ExperimentProcedures = NormalizeStrings(config?.ExperimentProcedures),
                DiscussionPrompt = config?.DiscussionPrompt?.Trim() ?? string.Empty,
                RealWorldApplications = NormalizeStrings(config?.RealWorldApplications),
                RelatedCareers = NormalizeStrings(config?.RelatedCareers),
                RealWorldTask = config?.RealWorldTask?.Trim() ?? string.Empty,
                Status = ils.Status == IlsStatus.Published ? "published" : "draft",
                CreatedBy = ils.CreatedBy,
                CreatedAt = ils.CreatedAt,
                UpdatedAt = ils.UpdatedAt
            };
        }

        private static List<string> NormalizeStrings(IEnumerable<string>? values) =>
            values?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
            ?? new List<string>();

        private static string FormatDurationHours(int durationMinutes)
        {
            var hours = Math.Round(durationMinutes / 60m, 2, MidpointRounding.AwayFromZero);
            return hours.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static bool TryParseDurationToMinutes(string? raw, out int durationMinutes)
        {
            durationMinutes = 0;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var input = raw.Trim().ToLowerInvariant();

            // Try to parse combined forms like "2 hours 30 minutes", or single units.
            var hourPattern = new Regex(@"(?<value>[-+]?\d*\.?\d+)\s*(h|hr|hrs|hour|hours)\b", RegexOptions.Compiled);
            var minutePattern = new Regex(@"(?<value>[-+]?\d*\.?\d+)\s*(m|min|mins|minute|minutes)\b", RegexOptions.Compiled);

            decimal totalMinutes = 0m;
            var matched = false;

            var hourMatch = hourPattern.Match(input);
            if (hourMatch.Success && decimal.TryParse(hourMatch.Groups["value"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var hours))
            {
                totalMinutes += hours * 60m;
                matched = true;
            }

            var minuteMatch = minutePattern.Match(input);
            if (minuteMatch.Success && decimal.TryParse(minuteMatch.Groups["value"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var mins))
            {
                totalMinutes += mins;
                matched = true;
            }

            if (!matched)
            {
                // If no explicit unit found, treat plain numeric as minutes (e.g., "30" => 30 minutes).
                if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric))
                    return false;
                if (numeric <= 0m)
                    return false;
                totalMinutes = numeric;
            }

            if (totalMinutes <= 0m)
                return false;

            durationMinutes = (int)Math.Round(totalMinutes, 0, MidpointRounding.AwayFromZero);
            return durationMinutes > 0;
        }

        private static IlsAssessmentConfig? ParseAssessmentConfig(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<IlsAssessmentConfig>(json, WebJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private sealed class IlsAssessmentConfig
        {
            public string? Score { get; set; }
            public string? IntroductionMessage { get; set; }
            public string? EngagementQuestion { get; set; }
            public string? HypothesisQuestion { get; set; }
            public List<string>? ExperimentProcedures { get; set; }
            public string? DiscussionPrompt { get; set; }
            public List<string>? RealWorldApplications { get; set; }
            public List<string>? RelatedCareers { get; set; }
            public string? RealWorldTask { get; set; }
            public QuizData? PreSimAssessment { get; set; }
            public QuizData? PostSimAssessment { get; set; }
        }

        private IActionResult Error(int statusCode, string code, string message, params (string field, string issue)[] details)
        {
            var payload = new
            {
                error = true,
                code,
                message,
                details = details.Select(d => new { field = d.field, issue = d.issue })
            };
            return StatusCode(statusCode, payload);
        }
    }
}
