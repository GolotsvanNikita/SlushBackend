using Slush.Application.DTOs.Auth;
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
    }
}
