using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.DTOs.Payment;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace BlueSandsLMS.Application.Services.PaymentsV2;

public sealed class PaymentsV2Service : IPaymentsV2Service
{
    private readonly IPaymentsV2Repository _repo;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;
    private readonly IPricingService _pricing;
    private readonly ICacheBustService? _cacheBust;
    private readonly ILogger<PaymentsV2Service>? _logger;
    private readonly string? _paystackSecret;
    private readonly HttpClient _http;

    private const decimal MinimumPremiumPerStudent = 5000m;

    public PaymentsV2Service(
        IPaymentsV2Repository repo,
        IHttpClientFactory httpFactory,
        IConfiguration cfg,
        IPricingService pricing,
        ICacheBustService? cacheBust = null,
        ILogger<PaymentsV2Service>? logger = null)
    {
        _repo = repo;
        _httpFactory = httpFactory;
        _cfg = cfg;
        _pricing = pricing;
        _cacheBust = cacheBust;
        _logger = logger;
        _paystackSecret = cfg["Payments:Paystack:SecretKey"];
        _http = httpFactory.CreateClient();
        _http.BaseAddress = new Uri("https://api.paystack.co/");

        if (!string.IsNullOrWhiteSpace(_paystackSecret))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _paystackSecret);
    }

    public async Task<SubscribeV2Response> SubscribeAsync(SubscribeV2Request req, ClaimsPrincipal user)
    {
        if (req.Students < 1) return new SubscribeV2Response(false, "Students must be >= 1.", null);
        if (string.IsNullOrWhiteSpace(req.ContactEmail)) return new SubscribeV2Response(false, "ContactEmail is required.", null);

        var plan = req.Plan?.Trim().ToLowerInvariant();
        if (plan == "free")
        {
            var now = DateTime.UtcNow;
            var sub = new Subscription
            {
                SchoolId = req.SchoolId,
                UserId = GetUserId(user),
                StudentsCovered = req.Students,
                PricePerStudent = 0m,
                StartsAt = now,
                EndsAt = now.AddMonths(1),
                Active = true,
                LastPaymentReference = "free"
            };

            await _repo.UpsertSubscriptionAsync(sub);

            if (req.SchoolId != Guid.Empty)
                _cacheBust?.InvalidateSchoolAdmin(req.SchoolId);

            return new SubscribeV2Response(true, "Free subscription activated.", null);
        }

        if (plan == "premium")
        {
            // Resolve recommended per-student then enforce minimum
            var now = DateTime.UtcNow;
            var (resolved, _) = await _pricing.ResolvePerStudentAsync(req.Students, now, req.PromoCode);
            var perStudent = Math.Max(MinimumPremiumPerStudent, resolved);
            // Compose init request (amount equals perStudent * students)
            var initReq = new InitPaymentV2Request(req.SchoolId, req.Students, req.ContactEmail, req.PromoCode);
            var init = await InitializePaymentAsync(initReq, user);

            return new SubscribeV2Response(true, "Payment initialized for premium plan.", new InitPaymentResponse(init.AuthorizationUrl, init.AccessCode, init.Reference));
        }

        return new SubscribeV2Response(false, "Invalid plan. Supported: free, premium.", null);
    }

    public async Task<InitPaymentV2Response> InitializePaymentAsync(InitPaymentV2Request req, ClaimsPrincipal user)
    {
        if (req.Students < 1) throw new ArgumentException("Students must be >= 1.");
        if (string.IsNullOrWhiteSpace(req.ContactEmail)) throw new ArgumentException("ContactEmail is required.");
        if (string.IsNullOrWhiteSpace(_paystackSecret)) throw new InvalidOperationException("Paystack not configured.");

        var now = DateTime.UtcNow;
        var (resolvedPerStudent, _) = await _pricing.ResolvePerStudentAsync(req.Students, now, req.PromoCode);
        var perStudent = Math.Max(MinimumPremiumPerStudent, resolvedPerStudent);

        var subtotal = perStudent * req.Students;
        var vat = _pricing.ComputeTotals(req.Students, perStudent, now).vat;
        var total = subtotal + vat;
        var amountKobo = (long)(total * 100m);

        var reference = $"V2-{Guid.NewGuid():N}";

        var p = new Payment
        {
            SchoolId = req.SchoolId,
            UserId = GetUserId(user),
            Provider = "paystack",
            Reference = reference,
            Currency = "NGN",
            AmountKobo = amountKobo,
            Subtotal = subtotal,
            Vat = vat,
            Total = total,
            StudentsBilled = req.Students,
            PricePerStudent = perStudent,
            Status = PaymentStatus.Pending,
            PromoCode = string.IsNullOrWhiteSpace(req.PromoCode) ? null : req.PromoCode.Trim()
        };

        await _repo.CreatePaymentAsync(p);

        var callbackUrl = _cfg["Payments:Paystack:CallbackUrl"];
        if (string.IsNullOrWhiteSpace(callbackUrl))
            throw new InvalidOperationException("Payments:Paystack:CallbackUrl not configured.");

        var payload = new
        {
            email = req.ContactEmail.Trim(),
            amount = amountKobo,
            reference,
            currency = "NGN",
            callback_url = callbackUrl
        };

        var res = await _http.PostAsJsonAsync("transaction/initialize", payload);
        var json = await res.Content.ReadAsStringAsync();

        p.RawResponse = json;
        await _repo.UpdatePaymentAsync(p);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Paystack initialize failed: {json}");

        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        return new InitPaymentV2Response(
            data.GetProperty("authorization_url").GetString()!,
            data.GetProperty("access_code").GetString()!,
            data.GetProperty("reference").GetString()!
        );
    }

    public async Task<VerifyPaymentV2Response> VerifyPaymentAsync(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return new VerifyPaymentV2Response(false, reference, "invalid", "Reference is required.");

        reference = reference.Trim();
        var payment = await _repo.GetPaymentByReferenceAsync(reference);
        if (payment == null)
            return new VerifyPaymentV2Response(false, reference, "not_found", "Payment record not found.");

        if (payment.Status == PaymentStatus.Paid)
            return new VerifyPaymentV2Response(true, reference, "paid", "Payment already verified.");

        if (string.IsNullOrWhiteSpace(_paystackSecret))
            throw new InvalidOperationException("Paystack not configured.");

        // Call Paystack verify API with error handling
        HttpResponseMessage res;
        try
        {
            res = await _http.GetAsync($"transaction/verify/{reference}");
        }
        catch (HttpRequestException ex)
        {
            return new VerifyPaymentV2Response(false, reference, "network_error", $"Failed to reach Paystack: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return new VerifyPaymentV2Response(false, reference, "timeout", $"Paystack request timed out: {ex.Message}");
        }

        var json = await res.Content.ReadAsStringAsync();

        // Always persist raw response for auditing
        payment.RawResponse = json;
        await _repo.UpdatePaymentAsync(payment);

        // Check HTTP status before parsing JSON
        if (!res.IsSuccessStatusCode)
        {
            return new VerifyPaymentV2Response(false, reference, "api_error", $"Paystack returned {(int)res.StatusCode}: {json}");
        }

        // Parse JSON safely
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return new VerifyPaymentV2Response(false, reference, "parse_error", $"Invalid JSON from Paystack: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;

            // Determine where the transaction object is (root or root.data)
            JsonElement tx;
            if (root.TryGetProperty("data", out var dataElem))
                tx = dataElem;
            else
                tx = root;

            // Read status, amount and currency safely
            string? txStatus = null;
            if (tx.TryGetProperty("status", out var statusElem) && statusElem.ValueKind != JsonValueKind.Null)
            {
                if (statusElem.ValueKind == JsonValueKind.String)
                    txStatus = statusElem.GetString();
                else
                    txStatus = statusElem.ToString();
            }

            long txAmount = 0;
            if (tx.TryGetProperty("amount", out var amountElem) && amountElem.ValueKind != JsonValueKind.Null)
            {
                if (amountElem.ValueKind == JsonValueKind.Number && amountElem.TryGetInt64(out var a64))
                    txAmount = a64;
                else if (amountElem.ValueKind == JsonValueKind.String && long.TryParse(amountElem.GetString(), out var aParsed))
                    txAmount = aParsed;
            }

            string? txCurrency = null;
            if (tx.TryGetProperty("currency", out var currencyElem) && currencyElem.ValueKind == JsonValueKind.String)
                txCurrency = currencyElem.GetString();

            var expectedCurrency = string.IsNullOrWhiteSpace(payment.Currency) ? "NGN" : payment.Currency;
            var ok = string.Equals(txStatus, "success", StringComparison.OrdinalIgnoreCase)
                     && txAmount == payment.AmountKobo
                     && string.Equals(txCurrency, expectedCurrency, StringComparison.OrdinalIgnoreCase);

            if (!ok)
            {
                // Read gateway response for more details
                string? gatewayResponse = null;
                if (tx.TryGetProperty("gateway_response", out var gwElem) && gwElem.ValueKind == JsonValueKind.String)
                    gatewayResponse = gwElem.GetString();

                // If transaction is explicitly failed/abandoned, mark payment failed
                if (!string.IsNullOrWhiteSpace(txStatus) &&
                    (string.Equals(txStatus, "failed", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(txStatus, "abandoned", StringComparison.OrdinalIgnoreCase)))
                {
                    if (payment.Status != PaymentStatus.Failed)
                    {
                        payment.Status = PaymentStatus.Failed;
                        await _repo.UpdatePaymentAsync(payment);
                    }

                    var message = $"Transaction {txStatus.ToLowerInvariant()}";
                    if (!string.IsNullOrWhiteSpace(gatewayResponse))
                        message += $": {gatewayResponse}";

                    return new VerifyPaymentV2Response(false, reference, txStatus?.ToLowerInvariant(), message);
                }

                // Not enough evidence to mark paid; return false with status info
                var statusInfo = string.IsNullOrWhiteSpace(txStatus) ? "unknown" : txStatus.ToLowerInvariant();
                return new VerifyPaymentV2Response(false, reference, statusInfo, $"Verification pending. Status: {txStatus ?? "unknown"}");
            }

            // Mark as paid and activate subscription (idempotent)
            if (payment.Status != PaymentStatus.Paid)
            {
                payment.Status = PaymentStatus.Paid;
                await _repo.UpdatePaymentAsync(payment);

                var now = DateTime.UtcNow;
                var sub = new Subscription
                {
                    SchoolId = payment.SchoolId,
                    UserId = payment.UserId,
                    StudentsCovered = payment.StudentsBilled,
                    PricePerStudent = payment.PricePerStudent,
                    StartsAt = now,
                    EndsAt = now.AddMonths(1),
                    Active = true,
                    LastPaymentReference = payment.Reference
                };

                await _repo.UpsertSubscriptionAsync(sub);

                if (payment.SchoolId != Guid.Empty)
                    _cacheBust?.InvalidateSchoolAdmin(payment.SchoolId);
            }

            return new VerifyPaymentV2Response(true, reference, "paid", "Payment verified successfully.");
        }
    }

    public async Task HandleWebhookAsync(string rawBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_paystackSecret)) throw new InvalidOperationException("Paystack not configured.");

        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        var keyBytes = Encoding.UTF8.GetBytes(_paystackSecret);

        using var hmac = new System.Security.Cryptography.HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(bodyBytes);
        var computed = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        var sigNormalized = signatureHeader?.Trim().ToLowerInvariant() ?? "";

        if (!string.Equals(computed, sigNormalized, StringComparison.Ordinal))
        {
            // Detailed diagnostics to pinpoint the mismatch
            _logger?.LogWarning(
                "Webhook signature MISMATCH.\n" +
                "  SecretKey: {KeyPrefix}... (length={KeyLen})\n" +
                "  Body bytes: {BodyLen} (first 60 chars: {BodyPreview})\n" +
                "  Computed HMAC: {Computed}\n" +
                "  Received header: {Received}\n" +
                "  Match: {Match}",
                _paystackSecret.Substring(0, Math.Min(10, _paystackSecret.Length)),
                _paystackSecret.Length,
                bodyBytes.Length,
                rawBody.Length > 60 ? rawBody[..60] : rawBody,
                computed[..Math.Min(32, computed.Length)],
                sigNormalized.Length > 32 ? sigNormalized[..32] : sigNormalized,
                computed == sigNormalized
            );
            throw new UnauthorizedAccessException("Invalid webhook signature.");
        }

        _logger?.LogInformation("Webhook signature verified successfully. Body length: {BodyLen}", bodyBytes.Length);

        using var doc = JsonDocument.Parse(rawBody);
        var eventName = doc.RootElement.GetProperty("event").GetString();
        if (eventName == "charge.success")
        {
            var data = doc.RootElement.GetProperty("data");
            var reference = data.GetProperty("reference").GetString()!;
            
            // Use GetInt64 to avoid overflow for large amounts (> 21M kobo)
            long amount;
            var amountElem = data.GetProperty("amount");
            if (amountElem.ValueKind == JsonValueKind.Number && amountElem.TryGetInt64(out var a64))
                amount = a64;
            else if (amountElem.ValueKind == JsonValueKind.String && long.TryParse(amountElem.GetString(), out var aParsed))
                amount = aParsed;
            else
                amount = 0;

            var p = await _repo.GetPaymentByReferenceAsync(reference);
            if (p != null && p.AmountKobo == amount && p.Status != PaymentStatus.Paid)
            {
                p.Status = PaymentStatus.Paid;
                p.RawResponse = rawBody;
                await _repo.UpdatePaymentAsync(p);

                var now = DateTime.UtcNow;
                var sub = new Subscription
                {
                    SchoolId = p.SchoolId,
                    UserId = p.UserId,
                    StudentsCovered = p.StudentsBilled,
                    PricePerStudent = p.PricePerStudent,
                    StartsAt = now,
                    EndsAt = now.AddMonths(1),
                    Active = true,
                    LastPaymentReference = p.Reference
                };

                await _repo.UpsertSubscriptionAsync(sub);

                if (p.SchoolId != Guid.Empty)
                    _cacheBust?.InvalidateSchoolAdmin(p.SchoolId);
            }
        }
    }

    public async Task<SubscriptionV2Dto?> GetSubscriptionAsync(Guid schoolId)
    {
        var s = await _repo.GetActiveSubscriptionAsync(schoolId);
        if (s == null) return null;
        return new SubscriptionV2Dto(s.SchoolId, s.Active, s.StudentsCovered, s.PricePerStudent, s.StartsAt, s.EndsAt, s.LastPaymentReference);
    }

    public async Task<CancelSubscriptionV2Response> CancelSubscriptionAsync(Guid schoolId, ClaimsPrincipal user)
    {
        // Add authorization check as needed (e.g., ensure user belongs to school)
        await _repo.CancelSubscriptionAsync(schoolId);
        if (schoolId != Guid.Empty)
            _cacheBust?.InvalidateSchoolAdmin(schoolId);
        return new CancelSubscriptionV2Response(true, "Canceled");
    }

    private Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user?.FindFirst("sub")?.Value ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(sub, out var uid)) return uid;
        return null;
    }

    //private async Task TryIncrementPromoAsync(string? promoCode)
    //{
    //    if (string.IsNullOrWhiteSpace(promoCode)) return;
    //    var code = await _repo.GetType().Assembly // quick guard; ideally this logic lives in a promo repo
    //        .AsTask(); // no-op just to avoid unused warning; replace with your promo increment logic if desired
    //}
}