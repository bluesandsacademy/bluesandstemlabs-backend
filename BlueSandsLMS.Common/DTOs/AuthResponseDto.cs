namespace BlueSandsLMS.Common.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public DateTime? AccessTokenExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public Guid UserId { get; set; }
        public Guid? SchoolId { get; set; }
        public string? SchoolName { get; set; }
        public string SchoolCurrency { get; set; } = "NGN";

        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string? Country { get; set; }

        public bool PromoApplied { get; set; }
        public string? PromoMessage { get; set; }
        public SubscriptionSummaryDto? Subscription { get; set; }
        public TierSummaryDto? CurrentTier { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
    }
}
