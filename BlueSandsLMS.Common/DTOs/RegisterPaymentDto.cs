using System;

namespace BlueSandsLMS.Common.DTOs
{
    /// <summary>
    /// Manual payment registration for an individual user (non-Paystack).
    /// All amounts are in Naira (NGN).
    /// </summary>
    public sealed record RegisterPaymentRequest(
        string Reference,
        Guid UserId,
        int StudentCount,
        decimal PricePerStudent,
        decimal Subtotal,
        decimal VatAmount,
        decimal Amount,
        string? PromoCode = null
    );

    public sealed record RegisterPaymentResponse(
        string Reference,
        Guid UserId,
        int StudentCount,
        decimal Amount,
        DateTime ActivatedFromUtc,
        DateTime ActivatedToUtc,
        string Message
    );
}