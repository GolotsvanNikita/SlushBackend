using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Auth;
using Slush.Application.DTOs.Profile;
using Slush.Application.Interfaces;
using System.Security.Claims;

namespace Slush.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            try
            {
                await _authService.RegisterAsync(request);

                return Ok(new
                {
                    message = "Registration successful. Please check your email for the verification code."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyCodeDto request)
        {
            try
            {
                await _authService.VerifyEmailAsync(request);

                return Ok(new
                {
                    message = "Email verified successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid username or password.")
                {
                    return Unauthorized(new
                    {
                        message = ex.Message
                    });
                }

                if (ex.Message == "Please verify your email address.")
                {
                    return Unauthorized(new
                    {
                        message = ex.Message
                    });
                }

                if (ex.Message == "This account has been banned by an administrator.")
                {
                    return StatusCode(403, new
                    {
                        message = ex.Message
                    });
                }

                return StatusCode(500, new
                {
                    message = "An unexpected error occurred."
                });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request);

                return Ok(new
                {
                    message = "Password reset code sent to your email."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);

                return Ok(new
                {
                    message = "Password has been reset successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("resend-verification-code")]
        public async Task<IActionResult> ResendVerificationCode(
            [FromBody] ResendVerificationCodeDto request)
        {
            try
            {
                await _authService.ResendVerificationCodeAsync(request);

                return Ok(new
                {
                    message = "If the email is registered, a new code has been sent."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdString, out Guid userId) ? userId : Guid.Empty;
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                await _authService.ChangePasswordAsync(userId, request);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid current password.")
                    return Unauthorized(new { message = ex.Message });

                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                await _authService.DeleteAccountAsync(userId, request);
                return Ok(new { message = "Account successfully deleted." });
            }
            catch (Exception ex)
            {
                if (ex.Message == "Invalid password.")
                    return Unauthorized(new { message = ex.Message });

                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}