using System.Security.Claims;
using BlueSandsLMS.Common.DTOs;
using BlueSandsLMS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using BlueSandsLMS.Common.Interfaces;

namespace BlueSandsLMS.Application.Services
{

    public sealed partial class PaymentService
    {
        public async Task<RegisterPaymentResponse> RegisterManualAsync(RegisterPaymentRequest req, ClaimsPrincipal actor)
        {
            var now = DateTime.UtcNow;
            var reference = req.Reference?.Trim() ?? string.Empty;


            if (string.IsNullOrWhiteSpace(reference)) throw new Exception("Reference is required.");
            if (req.StudentCount <= 0) throw new Exception("StudentCount must be >= 1.");
            if (req.PricePerStudent < 0m || req.Subtotal < 0m || req.VatAmount < 0m || req.Amount <= 0m)
                throw new Exception("Amounts must be positive.");

            if (decimal.Round(req.StudentCount * req.PricePerStudent, 2) != decimal.Round(req.Subtotal, 2))
                throw new Exception("Subtotal mismatch: StudentCount * PricePerStudent != Subtotal.");

            if (decimal.Round(req.Subtotal + req.VatAmount, 2) != decimal.Round(req.Amount, 2))
                throw new Exception("Amount mismatch: Subtotal + VatAmount != Amount.");


            var (expectedPricePerStudent, _) = await _pricing.ResolvePerStudentAsync(req.StudentCount, now, req.PromoCode);
            var validatedPromoCode = string.IsNullOrWhiteSpace(req.PromoCode) ? null : req.PromoCode.Trim();

            if (decimal.Round(req.PricePerStudent, 2) != decimal.Round(expectedPricePerStudent, 2))
            {
                throw new Exception(
                    $"Invalid PricePerStudent. Expected {expectedPricePerStudent:N2} " +
                    $"{(validatedPromoCode != null ? $"with promo code '{validatedPromoCode}'" : "without promo code")}, " +
                    $"but got {req.PricePerStudent:N2}."
                );
            }


            var user = await _db.Users.AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == req.UserId)
        ?? throw new Exception("Target user not found.");

            var userSchoolId = user.SchoolId ?? Guid.Empty;


            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {

                await using var tx = await _db.Database.BeginTransactionAsync();

                try
                {

                    var existing = await _db.Payments.FirstOrDefaultAsync(p => p.Reference == reference);

                    if (existing != null)
                    {
                        if (existing.Status == PaymentStatus.Paid)
                        {
                            var sub = existing.SchoolId != Guid.Empty
                                ? await _db.Subscriptions
                                    .Where(s => s.SchoolId == existing.SchoolId && s.Active)
                                    .OrderByDescending(s => s.EndsAt)
                                    .FirstOrDefaultAsync()
                                : await _db.Subscriptions
                                    .Where(s => s.UserId == existing.UserId && s.Active)
                                    .OrderByDescending(s => s.EndsAt)
                                    .FirstOrDefaultAsync();

                            await tx.CommitAsync();
                            return new RegisterPaymentResponse(
                                existing.Reference, user.Id, req.StudentCount,
                                existing.Total, sub?.StartsAt ?? now, sub?.EndsAt ?? now,
                                "Reference already registered as Paid (idempotent)."
                            );
                        }


                        existing.Status = PaymentStatus.Paid;
                        existing.Total = req.Amount;
                        existing.AmountKobo = (long)(req.Amount * 100m);
                        existing.StudentsBilled = req.StudentCount;
                        existing.PricePerStudent = req.PricePerStudent;
                        existing.Subtotal = req.Subtotal;
                        existing.Vat = req.VatAmount;
                        existing.PromoCode = validatedPromoCode;
                        existing.RawResponse = "manual-registration";

                        await _db.SaveChangesAsync();

                        await ActivateSubscriptionForManualAsync(existing);
                        await TryIncrementCouponAsync(existing.PromoCode);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {

                        var payment = new Payment
                        {
                            UserId = user.Id,
                            SchoolId = userSchoolId,
                            Reference = reference,
                            Currency = "NGN",
                            Subtotal = req.Subtotal,
                            Vat = req.VatAmount,
                            Total = req.Amount,
                            AmountKobo = (long)(req.Amount * 100m),
                            StudentsBilled = req.StudentCount,
                            PricePerStudent = req.PricePerStudent,
                            PromoCode = validatedPromoCode,
                            Status = PaymentStatus.Paid,
                            RawResponse = "manual-registration",
                            DateCreated = now
                        };

                        _db.Payments.Add(payment);
                        await _db.SaveChangesAsync();

                        await ActivateSubscriptionForManualAsync(payment);
                        await TryIncrementCouponAsync(payment.PromoCode);
                        await _db.SaveChangesAsync();
                    }


                    var subscription = userSchoolId != Guid.Empty
                        ? await _db.Subscriptions
                            .Where(s => s.SchoolId == userSchoolId && s.Active)
                            .OrderByDescending(s => s.EndsAt)
                            .FirstOrDefaultAsync()
                        : await _db.Subscriptions
                            .Where(s => s.UserId == user.Id && s.Active)
                            .OrderByDescending(s => s.EndsAt)
                            .FirstOrDefaultAsync();

                    await tx.CommitAsync();

                    if (userSchoolId != Guid.Empty)
                        _cacheBust?.InvalidateSchoolAdmin(userSchoolId);

                    var startsAt = subscription?.StartsAt ?? now;
                    var endsAt = subscription?.EndsAt ?? now;

                    var message = userSchoolId != Guid.Empty
                        ? "Manual payment registered; school subscription activated."
                        : "Manual payment registered; individual subscription activated.";

                    if (validatedPromoCode != null)
                        message += $" Promo code '{validatedPromoCode}' applied (₦2,000 per student).";

                    return new RegisterPaymentResponse(reference, user.Id, req.StudentCount, req.Amount, startsAt, endsAt, message);
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

        }


        private async Task ActivateSubscriptionForManualAsync(Payment p)
        {
            var now = DateTime.UtcNow;

            var isSchool = p.SchoolId != Guid.Empty;
            Subscription? sub = isSchool
                ? await _db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == p.SchoolId && s.Active)
                : await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == p.UserId && s.Active);

            if (sub == null)
            {
                sub = new Subscription
                {
                    SchoolId = isSchool ? p.SchoolId : Guid.Empty,
                    UserId = isSchool ? null : p.UserId,
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
