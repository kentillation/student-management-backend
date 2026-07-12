using StudentManagementAPI.DTOs;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string ipAddress);
        Task<bool> LogoutAsync(string userId, string token);
        Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken, string ipAddress);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordConfirmDto resetPasswordDto);
        Task<bool> RevokeTokenAsync(string token, string ipAddress);
        Task<bool> ValidateTokenAsync(string token);

        // Additional helpers used by controllers
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(string userId);
        Task<IList<string>> GetRolesAsync(User user);
    }
}