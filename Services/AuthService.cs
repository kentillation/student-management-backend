using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentManagementAPI.Data;
using StudentManagementAPI.DTOs;
using StudentManagementAPI.Helpers;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Check if user exists
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    throw new Exception("An account with this email already exists.");
                }

                // Create user
                var user = new User
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName.Trim(),
                    LastName = registerDto.LastName.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Registration failed: {errors}");
                }

                // Assign default role
                await _userManager.AddToRoleAsync(user, "Student");

                // Generate token
                return await GenerateAuthResponseAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email: {Email}", registerDto.Email);
                throw;
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string ipAddress)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    throw new Exception("Invalid email or password.");
                }

                if (!user.IsActive)
                {
                    throw new Exception("Your account has been disabled. Please contact support.");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

                if (!result.Succeeded)
                {
                    if (result.IsLockedOut)
                    {
                        throw new Exception("Your account has been locked due to multiple failed attempts. Please try again later.");
                    }
                    throw new Exception("Invalid email or password.");
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate response
                var response = await GenerateAuthResponseAsync(user);
                
                // Save refresh token
                await SaveRefreshTokenAsync(user.Id, response.RefreshToken, ipAddress);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", loginDto.Email);
                throw;
            }
        }

        public async Task<bool> LogoutAsync(string userId, string token)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                // Revoke refresh tokens
                var refreshTokens = _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked);
                
                foreach (var rt in refreshTokens)
                {
                    rt.IsRevoked = true;
                    rt.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Update security stamp to invalidate JWT
                user.SecurityStamp = Guid.NewGuid().ToString();
                await _userManager.UpdateAsync(user);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken, string ipAddress)
        {
            try
            {
                // Validate refresh token
                var storedRefreshToken = _context.RefreshTokens
                    .FirstOrDefault(rt => rt.Token == refreshToken && !rt.IsRevoked);

                if (storedRefreshToken == null)
                {
                    throw new Exception("Invalid refresh token.");
                }

                if (storedRefreshToken.ExpiryDate < DateTime.UtcNow)
                {
                    throw new Exception("Refresh token has expired.");
                }

                var user = await _userManager.FindByIdAsync(storedRefreshToken.UserId);
                if (user == null || !user.IsActive)
                {
                    throw new Exception("User not found or inactive.");
                }

                // Revoke old refresh token
                storedRefreshToken.IsRevoked = true;
                storedRefreshToken.RevokedAt = DateTime.UtcNow;
                storedRefreshToken.RevokedByIp = ipAddress;

                // Generate new tokens
                var response = await GenerateAuthResponseAsync(user);
                
                // Save new refresh token
                await SaveRefreshTokenAsync(user.Id, response.RefreshToken, ipAddress);

                await _context.SaveChangesAsync();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token failed");
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.ChangePasswordAsync(
                    user, 
                    changePasswordDto.CurrentPassword, 
                    changePasswordDto.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Password change failed: {errors}");
                }

                // Revoke all refresh tokens on password change
                var refreshTokens = _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked);
                
                foreach (var rt in refreshTokens)
                {
                    rt.IsRevoked = true;
                    rt.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change failed for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null) return false;

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // In a real application, send this token via email
                // For now, we'll just log it
                _logger.LogInformation("Password reset token for {Email}: {Token}", email, token);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password failed for email: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordConfirmDto resetPasswordDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
                if (user == null) return false;

                var result = await _userManager.ResetPasswordAsync(
                    user, 
                    resetPasswordDto.Token, 
                    resetPasswordDto.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Password reset failed for {Email}: {Errors}", resetPasswordDto.Email, errors);
                    return false;
                }

                // Revoke all refresh tokens on password reset
                var refreshTokens = _context.RefreshTokens
                    .Where(rt => rt.UserId == user.Id && !rt.IsRevoked);
                
                foreach (var rt in refreshTokens)
                {
                    rt.IsRevoked = true;
                    rt.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reset password failed for email: {Email}", resetPasswordDto.Email);
                return false;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token, string ipAddress)
        {
            try
            {
                var refreshToken = _context.RefreshTokens
                    .FirstOrDefault(rt => rt.Token == token && !rt.IsRevoked);

                if (refreshToken == null) return false;

                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Revoke token failed");
                return false;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);
                
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);
            
            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = GenerateRefreshToken(),
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                Roles = roles.ToList()
            };
        }

        private string GenerateJwtToken(User user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private async Task SaveRefreshTokenAsync(string userId, string refreshToken, string ipAddress)
        {
            var token = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                IsRevoked = false
            };

            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<IList<string>> GetRolesAsync(User user)
        {
            return await _userManager.GetRolesAsync(user);
        }
    }
}