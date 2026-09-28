using Slush.Application.DTOs.Auth;
using Slush.Application.DTOs.Profile;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterDto request);
        Task<AuthResponseDto> LoginAsync(LoginDto request);
        Task VerifyEmailAsync(VerifyCodeDto request);
        Task ForgotPasswordAsync(ForgotPasswordDto request);
        Task ResetPasswordAsync(ResetPasswordDto request);
        Task ResendVerificationCodeAsync(ResendVerificationCodeDto request);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task ChangePasswordAsync(Guid userId, ChangePasswordDto request);
        Task DeleteAccountAsync(Guid userId, DeleteAccountDto request);
    }
}
