namespace BlueSandsLMS.Common.DTOs
{
    public sealed record CreateTierDto(string TierName, int MinStudents, int MaxStudents, decimal PricePerStudent);
    public sealed record TierListItemDto(long Id, string TierName, int MinStudents, int MaxStudents, decimal PricePerStudent);

    public sealed record PromoUpsertDto(bool UsePromoPricing, decimal PromoPricePerStudent, DateTime? StartsAt, DateTime? EndsAt);

    public sealed record InitPaymentRequest(Guid SchoolId, int Students, string ContactEmail, string? PromoCode);
    public sealed record InitPaymentResponse(string authorization_url, string access_code, string reference);
    public sealed record VerifyPaymentResponse(bool ok, string reference);
}
