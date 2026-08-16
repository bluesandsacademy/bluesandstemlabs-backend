using BlueSandsLMS.Core.Entities;

namespace BlueSandsLMS.Common.Interfaces;

public interface IPaymentsV2Repository
{
    Task<Payment> CreatePaymentAsync(Payment payment);
    Task UpdatePaymentAsync(Payment payment);
    Task<Payment?> GetPaymentByReferenceAsync(string reference);
    Task<Subscription?> GetActiveSubscriptionAsync(Guid schoolId);
    Task UpsertSubscriptionAsync(Subscription subscription);
    Task CancelSubscriptionAsync(Guid schoolId);
}