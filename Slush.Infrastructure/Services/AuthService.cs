using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Slush.Application.DTOs.Auth;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Slush.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(IUnitOfWork uow, IEmailService emailService, IConfiguration configuration)
    {
        _uow = uow;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task RegisterAsync(RegisterDto request)
    {
        var userRepository = _uow.Repository<User>();

        bool userExists = await userRepository.AsQueryable()
            .AnyAsync(u => u.Email == request.Email || u.Username == request.Username);

        if (userExists) throw new Exception("User with this username or email already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsEmailVerified = false
        };

        await userRepository.AddAsync(user);
        await _uow.SaveChangesAsync();

        string code = new Random().Next(10000, 99999).ToString();
        var verificationCode = new VerificationCode
        {
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Purpose = CodePurpose.EmailVerification
        };

        await _uow.Repository<VerificationCode>().AddAsync(verificationCode);
        await _uow.SaveChangesAsync();

        string emailBody = $@"
            <div style='font-family: Arial, sans-serif; color: #fff; background-color: #0d1b2a; padding: 20px; border-radius: 10px;'>
                <h2 style='color: #00f5d4;'>Welcome to Slush!</h2>
                <p>To continue your registration, please enter the following 5-digit code:</p>
                <h1 style='background-color: #1b263b; padding: 10px; display: inline-block; border-radius: 5px; letter-spacing: 5px;'>{code}</h1>
                <p>This code will expire in 15 minutes.</p>
            </div>";
        await _emailService.SendEmailAsync(user.Email, "Slush - Email Verification", emailBody);
    }

    public async Task VerifyEmailAsync(VerifyCodeDto request)
    {
        var user = await _uow.Repository<User>().AsQueryable()
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) throw new Exception("User not found.");

        var codeRepo = _uow.Repository<VerificationCode>();
        var verificationCode = await codeRepo.AsQueryable()
            .FirstOrDefaultAsync(vc => vc.UserId == user.Id && vc.Code == request.Code && vc.Purpose == CodePurpose.EmailVerification);

        if (verificationCode == null) throw new Exception("Invalid verification code.");
        if (verificationCode.ExpiresAt < DateTime.UtcNow) throw new Exception("Verification code has expired.");

        user.IsEmailVerified = true;

        _uow.Repository<User>().Update(user);
        codeRepo.Remove(verificationCode);

        await _uow.SaveChangesAsync();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request)
    {
        var user = await _uow.Repository<User>().AsQueryable()
            .FirstOrDefaultAsync(u => u.Email == request.LoginOrEmail || u.Username == request.LoginOrEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid username or password.");

        if (!user.IsEmailVerified) throw new Exception("Please verify your email address.");
        if (user.IsBanned) throw new Exception("This account has been banned by an administrator.");

        user.LastLoginAt = DateTime.UtcNow;
        _uow.Repository<User>().Update(user);

        var loginHistory = new UserLoginHistory
        {
            UserId = user.Id,
            LoginTimestamp = DateTime.UtcNow
        };
        await _uow.Repository<UserLoginHistory>().AddAsync(loginHistory);

        string accessToken = GenerateJwtToken(user);
        string refreshToken = GenerateRefreshTokenString();

        var newRefreshToken = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        await _uow.Repository<RefreshToken>().AddAsync(newRefreshToken);

        await _uow.SaveChangesAsync();

        return new AuthResponseDto(accessToken, refreshToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto request)
    {
        var user = await _uow.Repository<User>().AsQueryable()
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return;

        string code = new Random().Next(10000, 99999).ToString();

        var verificationCode = new VerificationCode
        {
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Purpose = CodePurpose.PasswordReset
        };

        await _uow.Repository<VerificationCode>().AddAsync(verificationCode);
        await _uow.SaveChangesAsync();

        string emailBody = $"<h2>Password Reset</h2><p>Your password reset code is: <b>{code}</b></p>";
        await _emailService.SendEmailAsync(user.Email, "Slush - Password Reset", emailBody);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request)
    {
        var user = await _uow.Repository<User>().AsQueryable()
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) throw new Exception("User not found.");

        var codeRepo = _uow.Repository<VerificationCode>();
        var verificationCode = await codeRepo.AsQueryable()
            .FirstOrDefaultAsync(vc => vc.UserId == user.Id && vc.Code == request.Code && vc.Purpose == CodePurpose.PasswordReset);

        if (verificationCode == null) throw new Exception("Invalid reset code.");
        if (verificationCode.ExpiresAt < DateTime.UtcNow) throw new Exception("Reset code has expired.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        _uow.Repository<User>().Update(user);
        codeRepo.Remove(verificationCode);

        await _uow.SaveChangesAsync();
    }

    public async Task ResendVerificationCodeAsync(ResendVerificationCodeDto request)
    {
        var user = await _uow.Repository<User>().AsQueryable()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null) return;
        if (user.IsEmailVerified) throw new Exception("Email is already verified.");

        var codeRepo = _uow.Repository<VerificationCode>();
        var existingCodes = await codeRepo.AsQueryable()
            .Where(vc => vc.UserId == user.Id && vc.Purpose == CodePurpose.EmailVerification)
            .ToListAsync();

        if (existingCodes.Any())
        {
            foreach (var code in existingCodes)
            {
                codeRepo.Remove(code);
            }
        }

        string newCode = new Random().Next(10000, 99999).ToString();

        var verificationCode = new VerificationCode
        {
            UserId = user.Id,
            Code = newCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Purpose = CodePurpose.EmailVerification
        };

        await codeRepo.AddAsync(verificationCode);
        await _uow.SaveChangesAsync();

        string emailBody = $@"
        <div style='font-family: Arial, sans-serif; color: #fff; background-color: #0d1b2a; padding: 20px; border-radius: 10px;'>
            <h2 style='color: #00f5d4;'>New Verification Code</h2>
            <p>Here is your new 5-digit code:</p>
            <h1 style='background-color: #1b263b; padding: 10px; display: inline-block; border-radius: 5px; letter-spacing: 5px;'>{newCode}</h1>
            <p>This code will expire in 15 minutes.</p>
        </div>";

        await _emailService.SendEmailAsync(user.Email, "Slush - New Email Verification Code", emailBody);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryMinutes"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var refreshTokenRepo = _uow.Repository<RefreshToken>();

        var existingToken = await refreshTokenRepo.AsQueryable()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (existingToken == null || !existingToken.IsActive)
            throw new Exception("Invalid or expired refresh token. Please login again.");

        var user = existingToken.User;
        if (user.IsBanned) throw new Exception("This account is banned.");

        existingToken.Revoked = DateTime.UtcNow;
        refreshTokenRepo.Update(existingToken);

        string newAccessToken = GenerateJwtToken(user);
        string newRefreshToken = GenerateRefreshTokenString();

        var newRefTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        await refreshTokenRepo.AddAsync(newRefTokenEntity);
        await _uow.SaveChangesAsync();

        return new AuthResponseDto(newAccessToken, newRefreshToken);
    }

    private string GenerateRefreshTokenString()
    {
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}