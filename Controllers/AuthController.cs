using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementAPI.DTOs;
using StudentManagementAPI.Models;
using StudentManagementAPI.Services;

namespace StudentManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _iAuthService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _iAuthService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _iAuthService.RegisterAsync(registerDto);
                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Registration successful! Please login."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email: {Email}", registerDto.Email);
                
                // Check for specific error types
                if (ex.Message.Contains("already exists"))
                {
                    return Conflict(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = "An account with this email already exists."
                    });
                }

                return BadRequest(new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Login user
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var result = await _iAuthService.LoginAsync(loginDto, ipAddress);
                
                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Login successful!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for email: {Email}", loginDto.Email);
                
                if (ex.Message.Contains("disabled"))
                {
                    return StatusCode(403, new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Your account has been disabled. Please contact support."
                    });
                }

                if (ex.Message.Contains("locked"))
                {
                    return StatusCode(403, new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Your account has been locked due to multiple failed attempts. Please try again later."
                    });
                }

                return Unauthorized(new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Logout user
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<bool>>> Logout()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Get the current token from the Authorization header
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                
                var result = await _iAuthService.LogoutAsync(userId, token);
                
                return Ok(new ApiResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Logged out successfully" : "Logout failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var result = await _iAuthService.RefreshTokenAsync(
                    refreshTokenDto.Token, 
                    refreshTokenDto.RefreshToken, 
                    ipAddress);
                
                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Token refreshed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token failed");
                return Unauthorized(new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Change password
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var result = await _iAuthService.ChangePasswordAsync(userId, changePasswordDto);
                
                return Ok(new ApiResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Password changed successfully" : "Password change failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password failed");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Forgot password - sends reset token to email
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                var result = await _iAuthService.ForgotPasswordAsync(forgotPasswordDto.Email);
                
                return Ok(new ApiResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Password reset instructions sent to your email" : "User not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password failed for email: {Email}", forgotPasswordDto.Email);
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordConfirmDto resetPasswordDto)
        {
            try
            {
                var result = await _iAuthService.ResetPasswordAsync(resetPasswordDto);
                
                return Ok(new ApiResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Password reset successfully" : "Password reset failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reset password failed");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Validate token
        /// </summary>
        [HttpPost("validate-token")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateToken([FromBody] string token)
        {
            try
            {
                var result = await _iAuthService.ValidateTokenAsync(token);
                
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = result ? "Token is valid" : "Token is invalid"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Revoke token
        /// </summary>
        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeToken([FromBody] string token)
        {
            try
            {
                var ipAddress = GetIpAddress();
                var result = await _iAuthService.RevokeTokenAsync(token, ipAddress);
                
                return Ok(new ApiResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Token revoked successfully" : "Token not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Revoke token failed");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                var user = await _iAuthService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var roles = await _iAuthService.GetRolesAsync(user);
                
                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Data = new AuthResponseDto
                    {
                        Email = user.Email ?? string.Empty,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Roles = roles.ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get current user failed");
                return BadRequest(new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        private string GetIpAddress()
        {
            var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            
            return ipAddress ?? "unknown";
        }
    }
}