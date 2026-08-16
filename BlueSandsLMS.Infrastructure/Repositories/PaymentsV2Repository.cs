using BlueSandsLMS.Common.Interfaces;
using BlueSandsLMS.Core.Entities;
using BlueSandsLMS.Infrastructure;
using Microsoft.EntityFrameworkCore;


namespace BlueSandsLMS.Infrastructure.Repositories;

public class PaymentsV2Repository : IPaymentsV2Repository
{
    private readonly BlueSandsLMSDbContext _db;
    public PaymentsV2Repository(BlueSandsLMSDbContext db) => _db = db;

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task UpdatePaymentAsync(Payment payment)
    {
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync();
    }

    public async Task<Payment?> GetPaymentByReferenceAsync(string reference)
    {
        return await _db.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
    }

    public async Task<Subscription?> GetActiveSubscriptionAsync(Guid schoolId)
    {
        return await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.SchoolId == schoolId && s.Active);
    }

    public async Task UpsertSubscriptionAsync(Subscription subscription)
    {
        var existing = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.SchoolId == subscription.SchoolId && s.Active);

        if (existing == null)
        {
            _db.Subscriptions.Add(subscription);
        }
        else
        {
            existing.StudentsCovered = subscription.StudentsCovered;
            existing.PricePerStudent = subscription.PricePerStudent;
            existing.StartsAt = subscription.StartsAt;
            existing.EndsAt = subscription.EndsAt;
            existing.Active = subscription.Active;
            existing.LastPaymentReference = subscription.LastPaymentReference;
            _db.Subscriptions.Update(existing);
        }

        await _db.SaveChangesAsync();
    }

    public async Task CancelSubscriptionAsync(Guid schoolId)
    {
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.SchoolId == schoolId && s.Active);
        if (sub != null)
        {
            sub.Active = false;
            await _db.SaveChangesAsync();
        }
    }
}