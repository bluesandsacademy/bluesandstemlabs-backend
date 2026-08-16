using BlueSandsLMS.Application.Services.PaymentsV2;
using BlueSandsLMS.Common.DTOs.Payment;
using BlueSandsLMS.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BlueSandsLMS.Api.Controllers;

[ApiController]
[Route("api/v2/payments")]
public sealed class PaymentsV2Controller : ControllerBase
{
    private readonly IPaymentsV2Service _svc;

    public PaymentsV2Controller(IPaymentsV2Service svc) => _svc = svc;

    [HttpPost("subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeV2Request req)
    {
        var res = await _svc.SubscribeAsync(req, User);
        if (!res.Success) return BadRequest(new { error = res.Message });
        return Ok(res);
    }

    [HttpPost("initialize")]
    [Authorize]
    public async Task<IActionResult> Initialize([FromBody] InitPaymentV2Request req)
    {
        try
        {
            var init = await _svc.InitializePaymentAsync(req, User);
            return Ok(new InitPaymentV2Response(init.AuthorizationUrl, init.AccessCode, init.Reference));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify([FromQuery] string reference, [FromServices] ILogger<PaymentsV2Controller> logger)
    {
        try
        {
            var res = await _svc.VerifyPaymentAsync(reference);
            
            // Return appropriate HTTP status based on verification outcome
            if (res.Ok)
                return Ok(res);
            
            if (res.Status == "not_found")
                return NotFound(res);
            
            if (res.Status == "network_error" || res.Status == "api_error" || res.Status == "parse_error")
                return StatusCode(502, res); // Bad Gateway - upstream payment provider issue
            
            // For abandoned/failed/pending - return 200 with details so frontend can act
            return Ok(res);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Verify failed for reference {Reference}: {Message}", reference, ex.Message);
            return StatusCode(503, new { ok = false, reference, status = "config_error", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during payment verification for reference {Reference}", reference);
            return StatusCode(500, new { ok = false, reference, status = "error", message = "An unexpected error occurred during verification. Please try again." });
        }
    }

    [HttpPost("webhook/paystack")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromServices] ILogger<PaymentsV2Controller> logger)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();
            var sig = Request.Headers["x-paystack-signature"].ToString();
            
            logger.LogInformation(
                "Webhook received. Body length: {BodyLen}, Signature length: {SigLen}, Content-Type: {ContentType}",
                raw.Length,
                sig.Length,
                Request.ContentType
            );
            
            await _svc.HandleWebhookAsync(raw, sig);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(401); // Standard HTTP 401 for invalid signature
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("subscription/{schoolId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetSubscription([FromRoute] Guid schoolId)
    {
        var s = await _svc.GetSubscriptionAsync(schoolId);
        if (s == null) return NotFound();
        return Ok(s);
    }

    [HttpPost("subscription/{schoolId:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelSubscription([FromRoute] Guid schoolId)
    {
        var res = await _svc.CancelSubscriptionAsync(schoolId, User);
        if (!res.Success) return BadRequest(new { error = res.Message });
        return Ok(res);
    }
}