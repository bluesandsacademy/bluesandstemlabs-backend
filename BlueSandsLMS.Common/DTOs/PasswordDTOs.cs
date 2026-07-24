


using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Common.DTOs
{

    public record RequestPasswordResetDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;
    }


    public record ResetPasswordDto
    {
        [Required]
        public string Token { get; init; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; init; } = string.Empty;


        public string? ConfirmPassword { get; init; }
    }


    public record ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; init; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
        public string NewPassword { get; init; } = string.Empty;


        public string? ConfirmPassword { get; init; }
    }

    public record RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; init; } = string.Empty;
    }
}
