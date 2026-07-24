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
        private readonly string? _paystackSecretKey;

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


            _paystackSecretKey = cfg["Payments:Paystack:SecretKey"];

            _http = httpFactory.CreateClient();
            _http.BaseAddress = new Uri("https://api.paystack.co/");

            if (!string.IsNullOrWhiteSpace(_paystackSecretKey))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _paystackSecretKey);
            }
        }

        public async Task<InitPaymentResponse> InitializeAsync(InitPaymentRequest req, ClaimsPrincipal? user)
        {
            if (_cfg.GetValue<bool>("Testing:AllowFakePaystackInit"))
            {
                if (req.Students < 1) throw new ArgumentException("Students must be >= 1.");
                if (string.IsNullOrWhiteSpace(req.ContactEmail)) throw new ArgumentException("ContactEmail is required.");

                var fakeReference = $"BS-TEST-{Guid.NewGuid():N}";
                return new InitPaymentResponse(
                    $"https://paystack.test/checkout/{fakeReference}",
                    "test_access_code",
                    fakeReference);
            }

            if (string.IsNullOrWhiteSpace(_paystackSecretKey))
                throw new InvalidOperationException(
                    "Paystack is not configured. Cannot initialize online payment. Please use manual payment registration instead.");

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


            string schoolCurrency = "NGN";
            if (req.SchoolId != Guid.Empty)
            {
                schoolCurrency = await _db.Schools
                    .Where(s => s.Id == req.SchoolId)
                    .Select(s => s.Currency)
                    .FirstOrDefaultAsync() ?? "NGN";
            }

            var p = new Payment
            {
                SchoolId = req.SchoolId,
                UserId = userId,
                Reference = reference,
                Currency = schoolCurrency,
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

            var callbackBase =
                (_cfg["Frontend:BaseUrl"] ??
                 _cfg["App:FrontendBaseUrl"] ??
                 "https://app.bluesandstemlabs.com").TrimEnd('/');

            var payload = new
            {
                email = req.ContactEmail.Trim(),
                amount = amountKobo,
                reference,
                currency = schoolCurrency,
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
            if (string.IsNullOrWhiteSpace(reference))
                throw new ArgumentException("Payment reference is required");

            reference = reference.Trim();


            var p = await _db.Payments.FirstOrDefaultAsync(x => x.Reference == reference);


            if (p == null) return new VerifyPaymentResponse(false, reference);


            if (p.Status == PaymentStatus.Paid)
                return new VerifyPaymentResponse(true, reference);


            if (string.Equals(p.RawResponse, "manual-registration", StringComparison.OrdinalIgnoreCase))
            {
                if (p.Status != PaymentStatus.Paid)
                {
                    p.Status = PaymentStatus.Paid;
                    await ActivateSubscriptionAsync(p);
                    await _db.SaveChangesAsync();

                    if (p.SchoolId != Guid.Empty)
                        _cacheBust?.InvalidateSchoolAdmin(p.SchoolId);
                }
                return new VerifyPaymentResponse(true, reference);
            }


            if (string.IsNullOrWhiteSpace(_paystackSecretKey))
                throw new InvalidOperationException("Paystack is not configured. Cannot verify online payment.");

            var res = await _http.GetAsync($"transaction/verify/{reference}");
            var json = await res.Content.ReadAsStringAsync();

            p.RawResponse = json;
            await _db.SaveChangesAsync();

            if (!res.IsSuccessStatusCode)
                return new VerifyPaymentResponse(false, reference);

            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");
            var status = data.GetProperty("status").GetString();
            var amount = data.GetProperty("amount").GetInt32();
            var currency = data.GetProperty("currency").GetString();


            var expectedCurrency = string.IsNullOrWhiteSpace(p.Currency) ? "NGN" : p.Currency;
            var ok = status == "success" &&
                     string.Equals(currency, expectedCurrency, StringComparison.OrdinalIgnoreCase) &&
                     amount == p.AmountKobo;
            if (!ok) return new VerifyPaymentResponse(false, reference);

            if (p.Status != PaymentStatus.Paid)
            {
                p.Status = PaymentStatus.Paid;
                await ActivateSubscriptionAsync(p);
                await TryIncrementCouponAsync(p.PromoCode);
                await _db.SaveChangesAsync();

                if (p.SchoolId != Guid.Empty)
                    _cacheBust?.InvalidateSchoolAdmin(p.SchoolId);
            }

            return new VerifyPaymentResponse(true, reference);
        }

        public async Task HandleWebhookAsync(string rawBody, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(_paystackSecretKey))
                throw new InvalidOperationException("Paystack is not configured. Cannot handle webhook.");


            using var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(_paystackSecretKey));
            var hash = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody)))
                .Replace("-", "").ToLowerInvariant();

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
                    await TryIncrementCouponAsync(p.PromoCode);
                    await _db.SaveChangesAsync();

                    if (p.SchoolId != Guid.Empty)
                        _cacheBust?.InvalidateSchoolAdmin(p.SchoolId);
                }
            }
        }


        private async Task TryIncrementCouponAsync(string? promoCode)
        {
            if (string.IsNullOrWhiteSpace(promoCode)) return;
            var code = await _db.PromoCodes.FirstOrDefaultAsync(c => c.Code == promoCode.Trim());
            if (code != null) code.RedemptionCount++;

        }


        private async Task ActivateSubscriptionAsync(Payment p)
        {
            var now = DateTime.UtcNow;

            var sub = await _db.Subscriptions
                .FirstOrDefaultAsync(s => s.SchoolId == p.SchoolId && s.Active);

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
