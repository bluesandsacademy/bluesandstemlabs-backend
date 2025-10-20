using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BlueSandsLMS.Application.Services
{
    public interface IPaymentService
    {
        Task<InitPaymentResponse> InitializeAsync(InitPaymentRequest req, ClaimsPrincipal? user);
        Task<VerifyPaymentResponse> VerifyAsync(string reference);
        Task HandleWebhookAsync(string rawBody, string signatureHeader);
        Task<RegisterPaymentResponse> RegisterManualAsync(RegisterPaymentRequest req, ClaimsPrincipal actor);
    }

    public sealed partial class PaymentService : IPaymentService
    {
        private readonly BlueSandsLMSDbContext _db;
        private readonly ICacheBustService _cacheBust;
        private readonly IPricingService _pricing;
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public PaymentService(
            BlueSandsLMSDbContext db,
            ICacheBustService cacheBust,
            IPricingService pricing,
            IHttpClientFactory httpFactory,
            IConfiguration cfg)
        {
            _db = db;
            _pricing = pricing;
            _cfg = cfg;
              _cacheBust = cacheBust;

            _http = httpFactory.CreateClient();
            // ✅ correct Paystack base URL
            _http.BaseAddress = new Uri("https://api.paystack.co/");
            var secret = _cfg["Paystack:SecretKey"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Paystack:SecretKey is not configured. Set it via environment variables (web.config).");

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        }

        public async Task<InitPaymentResponse> InitializeAsync(InitPaymentRequest req, ClaimsPrincipal? user)
        {
            if (req.Students < 1) throw new ArgumentException("Students must be ≥ 1.");
            if (string.IsNullOrWhiteSpace(req.ContactEmail)) throw new ArgumentException("ContactEmail is required.");

            var asOf = DateTime.UtcNow;
            var (perStudent, _) = await _pricing.ResolvePerStudentAsync(req.Students, asOf, req.PromoCode);
            var (subtotal, vat, total) = _pricing.ComputeTotals(req.Students, perStudent, asOf);

            var amountKobo = (long)(total * 100m);
            var reference = $"BS-{Guid.NewGuid():N}";

            Guid? userId = null;
            var sub = user?.FindFirst("sub")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(sub, out var uid)) userId = uid;

            var p = new Payment
            {
                SchoolId = req.SchoolId,
                UserId = userId,
                Reference = reference,
                AmountKobo = amountKobo,
                Subtotal = subtotal,
                Vat = vat,
                Total = total,
                StudentsBilled = req.Students,
                PricePerStudent = perStudent,
                PromoCode = string.IsNullOrWhiteSpace(req.PromoCode) ? null : req.PromoCode.Trim(),
                Status = PaymentStatus.Pending
            };
            _db.Payments.Add(p);
            await _db.SaveChangesAsync();

            // ✅ Frontend URL fallback (supports both keys)
            var callbackBase =
    (_cfg["Frontend:BaseUrl"] ??
     _cfg["App:FrontendBaseUrl"] ??
     "https://app.bluesandstemlabs.com").TrimEnd('/');

            var payload = new
            {
                email = req.ContactEmail.Trim(),
                amount = amountKobo,     // kobo
                reference,
                currency = "NGN",
                callback_url = $"{callbackBase}/billing/verify"
            };

            var res = await _http.PostAsJsonAsync("transaction/initialize", payload);
            var json = await res.Content.ReadAsStringAsync();
            p.RawResponse = json;
            await _db.SaveChangesAsync();

            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Paystack initialize failed: {json}");

            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");
            return new InitPaymentResponse(
                data.GetProperty("authorization_url").GetString()!,
                data.GetProperty("access_code").GetString()!,
                data.GetProperty("reference").GetString()!
            );
        }

        public async Task<VerifyPaymentResponse> VerifyAsync(string reference)
        {
            var res = await _http.GetAsync($"transaction/verify/{reference}");
            var json = await res.Content.ReadAsStringAsync();

            var p = await _db.Payments.FirstOrDefaultAsync(x => x.Reference == reference);
            if (p != null) { p.RawResponse = json; await _db.SaveChangesAsync(); }

            if (!res.IsSuccessStatusCode) return new VerifyPaymentResponse(false, reference);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var status   = root.GetProperty("data").GetProperty("status").GetString();   // "success"
            var amount   = root.GetProperty("data").GetProperty("amount").GetInt32();    // kobo
            var currency = root.GetProperty("data").GetProperty("currency").GetString();

            var ok = status == "success" && currency == "NGN" && p != null && amount == p.AmountKobo;
            if (ok && p!.Status != PaymentStatus.Paid)
            {
                p.Status = PaymentStatus.Paid;
                await ActivateSubscriptionAsync(p);
                await _db.SaveChangesAsync();
            }
            return new VerifyPaymentResponse(ok, reference);
        }

        public async Task HandleWebhookAsync(string rawBody, string signatureHeader)
        {
            // x-paystack-signature = HMAC-SHA512(rawBody, SecretKey)
            var secret = _cfg["Paystack:SecretKey"] ?? throw new InvalidOperationException("Missing Paystack SecretKey");
            using var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).Replace("-", "").ToLowerInvariant();
            if (!string.Equals(hash, signatureHeader, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Invalid webhook signature.");

            using var doc = JsonDocument.Parse(rawBody);
            if (doc.RootElement.GetProperty("event").GetString() == "charge.success")
            {
                var data = doc.RootElement.GetProperty("data");
                var reference = data.GetProperty("reference").GetString()!;
                var amount = data.GetProperty("amount").GetInt32();

                var p = await _db.Payments.FirstOrDefaultAsync(x => x.Reference == reference);
                if (p != null && amount == p.AmountKobo && p.Status != PaymentStatus.Paid)
                {
                    p.Status = PaymentStatus.Paid;
                    p.RawResponse = rawBody;
                    await ActivateSubscriptionAsync(p);
                    await _db.SaveChangesAsync();
                }
            }
        }

        // 1-month entitlement window (adjust as needed)
        private async Task ActivateSubscriptionAsync(Payment p)
        {
            var now = DateTime.UtcNow;
            var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == p.SchoolId && s.Active);
            if (sub == null)
            {
                sub = new Subscription
                {
                    SchoolId = p.SchoolId,
                    StudentsCovered = p.StudentsBilled,
                    PricePerStudent = p.PricePerStudent,
                    StartsAt = now,
                    EndsAt = now.AddMonths(1),
                    Active = true,
                    LastPaymentReference = p.Reference
                };
                _db.Subscriptions.Add(sub);
            }
            else
            {
                sub.StudentsCovered = p.StudentsBilled;
                sub.PricePerStudent = p.PricePerStudent;
                sub.StartsAt = now;
                sub.EndsAt = now.AddMonths(1);
                sub.Active = true;
                sub.LastPaymentReference = p.Reference;
            }
        }
    }
}
