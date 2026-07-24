using BlueSandsLMS.Common.DTOs;

namespace BlueSandsLMS.Application.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto, string? origin = null);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> LoginForRoleAsync(LoginDto dto, string requiredRole, int tokenExpiryHours);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<AuthResponseDto> AdminCreateUserAsync(AdminCreateUserDto dto);
        Task<AuthResponseDto> RegisterSchoolAsync(RegisterSchoolDto dto, string? origin = null);

        Task ResendVerificationAsync(string email, string? origin = null);

        Task VerifyEmailAsync(string token);
        Task RequestPasswordResetAsync(string email, string? origin = null);
        Task ResetPasswordAsync(string token, string newPassword, string? origin = null);
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string? origin = null);


        Task<AuthResponseDto> GoogleSignInAsync(string idToken, CancellationToken ct = default);
        Task<AuthResponseDto> GoogleSignUpAsync(GoogleSignUpDto dto, string? origin = null, CancellationToken ct = default);
    }
}
