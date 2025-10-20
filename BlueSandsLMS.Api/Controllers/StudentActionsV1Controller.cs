// Api/Controllers/StudentActionsV1Controller.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlueSandsLMS.Common.DTOs.Student;
using BlueSandsLMS.Common.Interfaces.Student;
using System.Security.Claims;

[ApiController]
[Route("api/student/v1")]
[Authorize(Roles = "Student,SchoolAdmin")]
public class StudentActionsV1Controller : ControllerBase
{
    private readonly IStudentActionsService _svc;
    public StudentActionsV1Controller(IStudentActionsService svc) { _svc = svc; }

   private Guid Me()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var id))
        {
            // Prefer returning 401 instead of throwing 500
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            // Throw to short-circuit the action; DevExceptionPage will show 401 status
            throw new InvalidOperationException("Missing or invalid user id claim.");
        }
        return id;
    }

    [HttpPost("experiments/start")]
    public async Task<ActionResult<StartExperimentResponse>> Start([FromBody] StartExperimentRequest req, CancellationToken ct)
        => Ok(await _svc.StartExperimentAsync(Me(), req, ct));

    [HttpPut("experiments/{launchId:guid}/progress")]
    public async Task<IActionResult> Progress([FromRoute] Guid launchId, [FromBody] SaveExperimentProgressRequest req, CancellationToken ct)
    { await _svc.SaveExperimentProgressAsync(Me(), launchId, req, ct); return NoContent(); }

    [HttpPut("experiments/{launchId:guid}/complete")]
    public async Task<IActionResult> Complete([FromRoute] Guid launchId, [FromBody] CompleteExperimentRequest req, CancellationToken ct)
    { await _svc.CompleteExperimentAsync(Me(), launchId, req, ct); return NoContent(); }

    [HttpPost("quizzes/submit")]
    public async Task<ActionResult<SubmitQuizResponse>> Submit([FromBody] SubmitQuizRequest req, CancellationToken ct)
        => Ok(await _svc.SubmitQuizAsync(Me(), req, ct));
}
