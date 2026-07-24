using System.Security.Claims;
using System.Text.Json;
using BlueSandsLMS.Api.Infrastructure;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSandsLMS.Api.Controllers
{
    [ApiController]
    [Route("api/session")]
    [Authorize(Roles = "Student")]
    public sealed class SessionController : ControllerBase
    {
        private static readonly string[] DefaultPollOptions =
        {
            "Strongly Disagree",
            "Disagree",
            "Agree",
            "Strongly Agree"
        };

        private static readonly JsonSerializerOptions AssessmentJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly HashSet<string> AllowedUploadMimes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "application/pdf"
        };

        private const long MaxUploadBytes = 10 * 1024 * 1024;

        private readonly BlueSandsLMSDbContext _db;
        private readonly IPlainTextInputGuard _inputGuard;
        private readonly ILogger<SessionController> _logger;

        public SessionController(
            BlueSandsLMSDbContext db,
            IPlainTextInputGuard inputGuard,
            ILogger<SessionController> logger)
        {
            _db = db;
            _inputGuard = inputGuard;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request, CancellationToken ct)
        {
            if (request.StudentId == Guid.Empty || request.IlsId == Guid.Empty)
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("studentId", "StudentId is required"),
                    ("ilsId", "IlsId is required"));
            }

            var tokenUserId = CurrentUserId();
            if (tokenUserId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");
            if (tokenUserId != request.StudentId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Token user must match studentId.");

            var ilsExists = await _db.InteractiveLearningSpaces
                .AnyAsync(x => x.Id == request.IlsId && x.Status == IlsStatus.Published, ct);
            if (!ilsExists)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Published ILS not found.");

            var existing = await _db.StudentIlsSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.StudentId == request.StudentId && x.IlsId == request.IlsId, ct);
            if (existing != null)
                return Ok(new { sessionId = existing.Id, step = existing.CurrentStep });

            var now = DateTime.UtcNow;
            var session = new StudentIlsSession
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                IlsId = request.IlsId,
                CurrentStep = 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.StudentIlsSessions.Add(session);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                var concurrent = await _db.StudentIlsSessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.StudentId == request.StudentId && x.IlsId == request.IlsId, ct);
                if (concurrent != null)
                    return Ok(new { sessionId = concurrent.Id, step = concurrent.CurrentStep });

                throw;
            }

            return StatusCode(StatusCodes.Status201Created, new { sessionId = session.Id, step = 1 });
        }

        [HttpPost("{id:guid}/poll")]
        public async Task<IActionResult> SubmitPoll(Guid id, [FromBody] PollSubmissionRequest request, CancellationToken ct)
        {
            var session = await _db.StudentIlsSessions
                .Include(x => x.Ils)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            var stepError = ValidateStepGate(session, expectedStep: 1, stepName: "poll");
            if (stepError != null) return stepError;

            var existing = await _db.SessionPolls.FirstOrDefaultAsync(x => x.SessionId == id, ct);
            if (existing != null)
                return Error(StatusCodes.Status409Conflict, "CONFLICT", "Poll has already been submitted for this session.");

            var poll = new SessionPoll
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                SubmittedAt = DateTime.UtcNow
            };

            if (request.Answers != null && request.Answers.Count > 0)
            {

                poll.OptionIndex = request.Answers[0].OptionIndex;
                poll.QuizTitle = request.QuizTitle;
                poll.TimeSpentSeconds = request.TimeSpentSeconds;
                poll.AnswersJson = JsonSerializer.Serialize(request.Answers);
                poll.Score = request.Score;
                poll.CorrectAnswers = request.CorrectAnswers;
                poll.TotalQuestions = request.TotalQuestions;
            }
            else
            {

                var options = ParsePollOptions(session.Ils?.PollOptionsJson);
                if (request.OptionIndex < 0 || request.OptionIndex >= options.Count)
                {
                    return Error(
                        StatusCodes.Status400BadRequest,
                        "VALIDATION_ERROR",
                        "OptionIndex is out of configured poll range.",
                        ("optionIndex", $"Expected range is 0 to {options.Count - 1}."));
                }
                poll.OptionIndex = request.OptionIndex;
            }

            _db.SessionPolls.Add(poll);

            session.CurrentStep = 2;
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { success = true, nextStep = 2 });
        }

        [HttpPost("{id:guid}/orientation")]
        public async Task<IActionResult> SubmitOrientation(Guid id, [FromBody] OrientationSubmissionRequest request, CancellationToken ct)
        {
            var session = await _db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            if (string.IsNullOrWhiteSpace(request.EngagementAnswer))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("engagementAnswer", "Engagement answer is required."));
            }

            if (!_inputGuard.IsSafe(request.EngagementAnswer))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("engagementAnswer", "Engagement answer contains unsupported characters or markup."));
            }

            var existing = await _db.SessionOrientations.FirstOrDefaultAsync(x => x.SessionId == id, ct);
            if (existing != null)
                return Error(StatusCodes.Status409Conflict, "CONFLICT", "Orientation has already been submitted for this session.");

            _db.SessionOrientations.Add(new SessionOrientation
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                EngagementAnswer = request.EngagementAnswer,
                SubmittedAt = DateTime.UtcNow
            });

            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { success = true, nextStep = session.CurrentStep });
        }

        [HttpPost("{id:guid}/realworld")]
        public async Task<IActionResult> SubmitRealWorld(Guid id, [FromBody] RealWorldSubmissionRequest request, CancellationToken ct)
        {
            var session = await _db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            if (string.IsNullOrWhiteSpace(request.Note))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("note", "Note is required."));
            }

            if (!_inputGuard.IsSafe(request.Note))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("note", "Note contains unsupported characters or markup."));
            }

            var existing = await _db.SessionRealWorlds.FirstOrDefaultAsync(x => x.SessionId == id, ct);
            if (existing != null)
                return Error(StatusCodes.Status409Conflict, "CONFLICT", "Real-world note has already been submitted for this session.");

            _db.SessionRealWorlds.Add(new SessionRealWorld
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                Note = request.Note,
                SubmittedAt = DateTime.UtcNow
            });

            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { success = true, nextStep = session.CurrentStep });
        }

        [HttpPost("{id:guid}/hypothesis")]
        public async Task<IActionResult> SubmitHypothesis(Guid id, [FromBody] HypothesisSubmissionRequest request, CancellationToken ct)
        {
            var session = await _db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            var stepError = ValidateStepGate(session, expectedStep: 2, stepName: "hypothesis");
            if (stepError != null) return stepError;

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("text", "Hypothesis text is required."));
            }

            if (!_inputGuard.IsSafe(request.Text))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("text", "Hypothesis text contains unsupported characters or markup."));
            }

            if (!string.Equals(request.InputMethod, "text", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.InputMethod, "voice", StringComparison.OrdinalIgnoreCase))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("inputMethod", "inputMethod must be 'text' or 'voice'."));
            }

            var existing = await _db.SessionHypotheses.FirstOrDefaultAsync(x => x.SessionId == id, ct);
            if (existing != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    error = true,
                    code = "CONFLICT",
                    message = "Hypothesis already exists for this session.",
                    hypothesisId = existing.Id
                });
            }

            var hypothesis = new SessionHypothesis
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                Text = request.Text,
                InputMethod = request.InputMethod.ToLowerInvariant(),
                SubmittedAt = DateTime.UtcNow
            };

            _db.SessionHypotheses.Add(hypothesis);
            session.CurrentStep = 3;
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { hypothesisId = hypothesis.Id, nextStep = 3 });
        }

        [HttpPost("{id:guid}/experiment")]
        public async Task<IActionResult> LogTrial(Guid id, [FromBody] ExperimentSubmissionRequest request, CancellationToken ct)
        {
            var session = await _db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            var stepError = ValidateStepGate(session, expectedStep: 3, stepName: "experiment");
            if (stepError != null) return stepError;

            if (request.Variables == null)
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("variables", "Variables are required."));
            }

            if (!string.IsNullOrWhiteSpace(request.ObservationText) && !_inputGuard.IsSafe(request.ObservationText))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("observationText", "Observation contains unsupported characters or markup."));
            }

            var resultScore = Math.Round(
                (request.Variables.Density * request.Variables.Volume) / Math.Max(1d, Math.Abs(request.Variables.Temp) + 1d),
                4);

            var resultPayload = new
            {
                score = resultScore,
                feedback = BuildExperimentFeedback(request.Variables),
                processedAt = DateTime.UtcNow
            };

            var variablesJson = JsonSerializer.Serialize(request.Variables);
            var resultJson = JsonSerializer.Serialize(resultPayload);
            var trial = new SessionTrial
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                VariablesJson = variablesJson,
                ObservationText = request.ObservationText,
                ResultJson = resultJson,
                CreatedAt = DateTime.UtcNow
            };

            _db.SessionTrials.Add(trial);
            session.LastSimulationStateJson = JsonSerializer.Serialize(new
            {
                canvasState = new
                {
                    variables = request.Variables,
                    score = resultScore
                },
                feedback = resultPayload.feedback
            });

            if (!string.IsNullOrWhiteSpace(request.ObservationText))
                session.CurrentStep = 4;

            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            var graphData = await BuildGraphData(id, ct);
            return Ok(new
            {
                trialId = trial.Id,
                graphData,
                nextStep = session.CurrentStep
            });
        }

        [HttpGet("{id:guid}/experiment")]
        public async Task<IActionResult> GetTrials(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            CancellationToken ct = default)
        {
            var session = await _db.StudentIlsSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            if (page <= 0 || pageSize <= 0 || pageSize > 200)
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("page", "page must be greater than 0."),
                    ("pageSize", "pageSize must be between 1 and 200."));
            }

            var totalCount = await _db.SessionTrials
                .AsNoTracking()
                .CountAsync(x => x.SessionId == id, ct);

            var trials = await _db.SessionTrials
                .AsNoTracking()
                .Where(x => x.SessionId == id)
                .OrderBy(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    trialId = x.Id,
                    variables = ParseJson(x.VariablesJson),
                    result = ParseJson(x.ResultJson),
                    observationText = x.ObservationText,
                    createdAt = x.CreatedAt
                })
                .ToListAsync(ct);

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
            return Ok(trials);
        }

        [HttpPost("{id:guid}/reflection")]
        public async Task<IActionResult> SubmitReflection(Guid id, [FromBody] ReflectionSubmissionRequest request, CancellationToken ct)
        {
            var session = await _db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            var stepError = ValidateStepGate(session, expectedStep: 4, stepName: "reflection");
            if (stepError != null) return stepError;

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("text", "Reflection text is required."));
            }

            if (!_inputGuard.IsSafe(request.Text))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("text", "Reflection text contains unsupported characters or markup."));
            }

            var existing = await _db.SessionReflections.AsNoTracking().FirstOrDefaultAsync(x => x.SessionId == id, ct);
            if (existing != null || session.ReflectionSubmitted)
                return Error(StatusCodes.Status409Conflict, "CONFLICT", "Reflection has already been submitted for this session.");

            var now = DateTime.UtcNow;
            var reflection = new SessionReflection
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                Text = request.Text,
                SubmittedAt = now
            };

            _db.SessionReflections.Add(reflection);
            session.ReflectionSubmitted = true;
            session.CurrentStep = 5;
            session.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);

            return Ok(new { reflectionId = reflection.Id });
        }

        [HttpPost("{id:guid}/assessment")]
        public async Task<IActionResult> SubmitAssessment(Guid id, [FromBody] AssessmentSubmissionRequest request, CancellationToken ct)
        {
            var session = await _db.StudentIlsSessions
                .Include(x => x.Ils)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            var existing = await _db.SessionAssessments.AsNoTracking().FirstOrDefaultAsync(x => x.SessionId == id, ct);
            if (existing != null)
            {
                return StatusCode(StatusCodes.Status409Conflict, new
                {
                    error = true,
                    code = "CONFLICT",
                    message = "Assessment has already been submitted for this session.",
                    score = existing.Score,
                    feedback = existing.Feedback,
                    badgeAwarded = session.BadgeAwarded,
                    completedAt = session.CompletedAt,
                    autoSubmitted = existing.IsAutoSubmitted
                });
            }

            var gateError = ValidateAssessmentGate(session);
            if (gateError != null) return gateError;

            var config = ParseAssessmentConfig(session.Ils?.AssessmentConfigJson);
            if (config == null || config.Questions.Count == 0)
            {
                return Error(
                    StatusCodes.Status422UnprocessableEntity,
                    "BUSINESS_RULE_ERROR",
                    "Assessment configuration is missing or invalid for this ILS.");
            }

            var answers = request.Answers;
            if ((answers == null || answers.Count == 0) && request.PostSimAnswers != null && request.PostSimAnswers.Count > 0)
            {
                answers = request.PostSimAnswers
                    .Where(a => a.QuestionIndex >= 0 && a.QuestionIndex < config.Questions.Count)
                    .Select(a => new AssessmentAnswerRequest
                    {
                        QId = config.Questions[a.QuestionIndex].QId,
                        Value = a.SelectedAnswer
                    })
                    .ToList();
            }

            if (answers == null || answers.Count == 0)
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("answers", "At least one answer is required."));
            }

            var scored = ScoreAssessment(config, answers);
            var now = DateTime.UtcNow;

            _db.SessionAssessments.Add(new SessionAssessment
            {
                Id = Guid.NewGuid(),
                SessionId = id,
                AnswersJson = JsonSerializer.Serialize(answers),
                Score = scored.Score,
                Feedback = scored.Feedback,
                IsAutoSubmitted = false,
                SubmittedAt = now
            });

            _db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                Utc = now,
                ActorUserId = session.StudentId,
                Category = "assessment",
                Name = "Session.AssessmentScored",
                ExtraJson = JsonSerializer.Serialize(new
                {
                    sessionId = session.Id,
                    score = scored.Score,
                    autoSubmitted = false,
                    timestamp = now
                })
            });

            if (session.CurrentStep == 5)
                session.CurrentStep = 6;

            session.CurrentStep = 7;
            session.CompletedAt = now;
            session.UpdatedAt = now;

            var hasHypothesis = await _db.SessionHypotheses.AsNoTracking().AnyAsync(x => x.SessionId == id, ct);
            var hasObservedTrial = await _db.SessionTrials.AsNoTracking()
                .AnyAsync(x => x.SessionId == id && !string.IsNullOrWhiteSpace(x.ObservationText), ct);
            var hasReflection = session.ReflectionSubmitted ||
                                await _db.SessionReflections.AsNoTracking().AnyAsync(x => x.SessionId == id, ct);
            var hasAssessment = true;

            var badgeAwarded = hasHypothesis && hasObservedTrial && hasReflection && hasAssessment;
            if (badgeAwarded)
            {
                session.BadgeAwarded = true;
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Assessment scored: sessionId={SessionId}, studentId={StudentId}, score={Score}, at={Timestamp}",
                session.Id,
                session.StudentId,
                scored.Score,
                now);

            return Ok(new
            {
                score = scored.Score,
                feedback = scored.Feedback,
                badgeAwarded = session.BadgeAwarded,
                completedAt = session.CompletedAt
            });
        }

        [HttpPost("{id:guid}/upload")]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<IActionResult> UploadSessionFile(Guid id, IFormFile? file, CancellationToken ct)
        {
            var session = await _db.StudentIlsSessions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (session == null)
                return Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Session not found.");

            var ownershipError = EnsureOwnership(session);
            if (ownershipError != null) return ownershipError;

            if (file == null || file.Length <= 0)
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("file", "A file upload is required."));
            }

            if (file.Length > MaxUploadBytes)
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("file", "Max file size is 10MB."));
            }

            await using var source = file.OpenReadStream();
            var detectedMime = await DetectMimeTypeAsync(source, ct);
            if (detectedMime == null || !AllowedUploadMimes.Contains(detectedMime))
            {
                return Error(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_ERROR",
                    "Validation failed.",
                    ("file", "Allowed file types: image/jpeg, image/png, application/pdf."));
            }

            var extension = detectedMime switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "application/pdf" => ".pdf",
                _ => string.Empty
            };

            var sessionFolder = Path.Combine("uploads", "sessions", id.ToString("N"));
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var physicalFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", sessionFolder);
            Directory.CreateDirectory(physicalFolder);
            var filePath = Path.Combine(physicalFolder, fileName);

            source.Position = 0;
            await using (var output = System.IO.File.Create(filePath))
            {
                await source.CopyToAsync(output, ct);
            }

            var relativeUrl = "/" + Path.Combine(sessionFolder, fileName).Replace("\\", "/");
            var fileUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

            session.UploadedFileUrl = fileUrl;
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { fileUrl });
        }

        private async Task<List<GraphPoint>> BuildGraphData(Guid sessionId, CancellationToken ct)
        {
            var trials = await _db.SessionTrials
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.ResultJson)
                .ToListAsync(ct);

            var points = new List<GraphPoint>(trials.Count);
            for (var i = 0; i < trials.Count; i++)
            {
                points.Add(new GraphPoint
                {
                    X = i + 1,
                    Y = ReadScore(trials[i])
                });
            }
            return points;
        }

        private IActionResult? ValidateStepGate(StudentIlsSession session, int expectedStep, string stepName)
        {
            if (session.CurrentStep < expectedStep)
            {
                return Error(
                    StatusCodes.Status422UnprocessableEntity,
                    "BUSINESS_RULE_ERROR",
                    $"Cannot submit {stepName} before reaching step {expectedStep}.");
            }

            if (session.CurrentStep > expectedStep)
            {
                return Error(
                    StatusCodes.Status409Conflict,
                    "CONFLICT",
                    $"{stepName} has already been completed for this session.");
            }

            return null;
        }

        private IActionResult? ValidateAssessmentGate(StudentIlsSession session)
        {
            if (session.CurrentStep < 5)
            {
                return Error(
                    StatusCodes.Status422UnprocessableEntity,
                    "BUSINESS_RULE_ERROR",
                    "Cannot submit assessment before reaching step 6.");
            }

            if (session.CurrentStep > 6)
            {
                return Error(
                    StatusCodes.Status409Conflict,
                    "CONFLICT",
                    "Assessment has already been completed for this session.");
            }

            return null;
        }

        private IActionResult? EnsureOwnership(StudentIlsSession session)
        {
            var userId = CurrentUserId();
            if (userId == Guid.Empty)
                return Error(StatusCodes.Status401Unauthorized, "AUTH_REQUIRED", "Authentication required.");
            if (session.StudentId != userId)
                return Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "You can only access your own session.");

            return null;
        }

        private Guid CurrentUserId()
        {
            var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
        }

        private static List<string> ParsePollOptions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return DefaultPollOptions.ToList();

            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list == null || list.Count == 0) return DefaultPollOptions.ToList();
                return list.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
            catch
            {
                return DefaultPollOptions.ToList();
            }
        }

        private static AssessmentConfig? ParseAssessmentConfig(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<AssessmentConfig>(json, AssessmentJsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static AssessmentScoreResult ScoreAssessment(AssessmentConfig config, List<AssessmentAnswerRequest> answers)
        {
            var map = answers
                .Where(a => !string.IsNullOrWhiteSpace(a.QId))
                .GroupBy(a => a.QId.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Last().Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            decimal earned = 0m;
            decimal possible = 0m;
            var feedback = new List<string>();

            foreach (var q in config.Questions.Where(q => !string.IsNullOrWhiteSpace(q.QId)))
            {
                var qPoints = q.Points <= 0 ? 1m : q.Points;
                possible += qPoints;

                if (!map.TryGetValue(q.QId.Trim(), out var answerText))
                {
                    if (!string.IsNullOrWhiteSpace(q.Feedback))
                        feedback.Add(q.Feedback.Trim());
                    continue;
                }

                var qEarned = ScoreQuestion(q, answerText, qPoints);
                earned += qEarned;

                if (qEarned < qPoints && !string.IsNullOrWhiteSpace(q.Feedback))
                    feedback.Add(q.Feedback.Trim());
            }

            var score = possible <= 0 ? 0m : decimal.Round(earned / possible, 4, MidpointRounding.AwayFromZero);
            if (score < 0m) score = 0m;
            if (score > 1m) score = 1m;

            var feedbackText = feedback.Count > 0
                ? string.Join(" ", feedback.Distinct(StringComparer.OrdinalIgnoreCase))
                : score >= 0.8m
                    ? "Strong result. Your assessment answers align with the experiment outcomes."
                    : "Assessment submitted. Review your trial observations and reflection for improvement.";

            return new AssessmentScoreResult(score, feedbackText);
        }

        private static decimal ScoreQuestion(AssessmentQuestion question, string answerText, decimal points)
        {
            var type = (question.Type ?? "mcq").Trim().ToLowerInvariant();
            if (type == "mcq")
            {
                if (string.IsNullOrWhiteSpace(question.CorrectAnswer))
                    return 0m;

                return string.Equals(answerText.Trim(), question.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase)
                    ? points
                    : 0m;
            }

            if (type == "short")
            {
                var expected = (question.ExpectedKeywords ?? new List<string>())
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Select(k => k.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                var answer = answerText.Trim().ToLowerInvariant();
                if (expected.Count > 0)
                {
                    var matched = expected.Count(k => answer.Contains(k, StringComparison.OrdinalIgnoreCase));
                    if (matched <= 0) return 0m;
                    if (matched >= expected.Count) return points;
                    return decimal.Round(points * matched / expected.Count, 4, MidpointRounding.AwayFromZero);
                }

                if (string.IsNullOrWhiteSpace(question.CorrectAnswer))
                    return 0m;

                return answer.Contains(question.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase)
                    ? points
                    : 0m;
            }

            return 0m;
        }

        private static async Task<string?> DetectMimeTypeAsync(Stream stream, CancellationToken ct)
        {
            var header = new byte[8];
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
            stream.Position = 0;

            if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return "image/jpeg";

            if (bytesRead >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                return "image/png";

            if (bytesRead >= 4 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
                return "application/pdf";

            return null;
        }

        private static string BuildExperimentFeedback(SimulationVariables variables)
        {
            if (variables.Temp >= 30)
                return "Increasing temperature causes molecules to move faster.";
            if (variables.Density >= 1.5)
                return "Higher density increases downward force in this setup.";
            if (variables.Volume >= 300)
                return "Larger volume amplifies the overall interaction response.";
            return "Variable update accepted. Keep testing combinations for clearer trends.";
        }

        private static object ParseJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(json);
            }
            catch
            {
                return new { raw = json };
            }
        }

        private static double ReadScore(string resultJson)
        {
            try
            {
                var element = JsonSerializer.Deserialize<JsonElement>(resultJson);
                if (element.TryGetProperty("score", out var score) && score.TryGetDouble(out var value))
                    return value;
            }
            catch
            {

            }
            return 0;
        }

        private sealed class AssessmentConfig
        {
            public List<AssessmentQuestion> Questions { get; set; } = new();
        }

        private sealed class AssessmentQuestion
        {
            public string QId { get; set; } = string.Empty;
            public string Type { get; set; } = "mcq";
            public decimal Points { get; set; } = 1m;
            public string? CorrectAnswer { get; set; }
            public List<string>? ExpectedKeywords { get; set; }
            public string? Feedback { get; set; }
        }

        private readonly record struct AssessmentScoreResult(decimal Score, string Feedback);

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
