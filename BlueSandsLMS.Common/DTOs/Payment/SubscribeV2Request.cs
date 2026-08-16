namespace BlueSandsLMS.Common.DTOs.Payment;

public sealed record SubscribeV2Request(
    Guid SchoolId,
    string Plan,         // "free" or "premium"
    int Students,
    string ContactEmail,
    string? PromoCode);

public sealed record SubscribeV2Response(bool Success, string Message, InitPaymentResponse? PaymentInit);

public sealed record InitPaymentV2Request(Guid SchoolId, int Students, string ContactEmail, string? PromoCode);
public sealed record InitPaymentV2Response(string AuthorizationUrl, string AccessCode, string Reference);

public sealed record VerifyPaymentV2Response(bool Ok, string Reference, string? Status = null, string? Message = null);

public sealed record SubscriptionV2Dto(
    Guid SchoolId,
    bool Active,
    int StudentsCovered,
    decimal PricePerStudent,
    DateTime StartsAt,
    DateTime EndsAt,
    string? LastPaymentReference);

public sealed record CancelSubscriptionV2Response(bool Success, string? Message);