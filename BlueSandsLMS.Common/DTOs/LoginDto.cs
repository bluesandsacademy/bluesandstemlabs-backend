using System.ComponentModel.DataAnnotations;

namespace BlueSandsLMS.Common.DTOs
{
    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }


    public record GoogleSignInDto
    {
        [Required]
        public string IdToken { get; init; } = string.Empty;
    }


    public record GoogleSignUpDto
    {
        [Required]
        public string IdToken { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? Country { get; init; }
        public string? CouponCode { get; init; }
    }
}
