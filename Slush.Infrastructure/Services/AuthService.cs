using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Slush.Application.DTOs.Auth;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;
using Slush.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Slush.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IEmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task RegisterAsync(RegisterDto request)
    {
        bool userExists = await _context.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username);
        if (userExists) throw new Exception("User with this username or email already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsEmailVerified = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        string code = new Random().Next(10000, 99999).ToString();
        _context.VerificationCodes.Add(new VerificationCode
        {
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Purpose = CodePurpose.EmailVerification
        });
        await _context.SaveChangesAsync();

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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) throw new Exception("User not found.");

        var verificationCode = await _context.VerificationCodes
            .FirstOrDefaultAsync(vc => vc.UserId == user.Id && vc.Code == request.Code && vc.Purpose == CodePurpose.EmailVerification);

        if (verificationCode == null) throw new Exception("Invalid verification code.");
        if (verificationCode.ExpiresAt < DateTime.UtcNow) throw new Exception("Verification code has expired.");

        user.IsEmailVerified = true;
        _context.VerificationCodes.Remove(verificationCode);
        await _context.SaveChangesAsync();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.LoginOrEmail || u.Username == request.LoginOrEmail);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid username or password.");

        if (!user.IsEmailVerified) throw new Exception("Please verify your email address.");

        string token = GenerateJwtToken(user);

        return new AuthResponseDto(token, "");
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return;

        string code = new Random().Next(10000, 99999).ToString();

        _context.VerificationCodes.Add(new VerificationCode
        {
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Purpose = CodePurpose.PasswordReset
        });
        await _context.SaveChangesAsync();

        string emailBody = $"<h2>Password Reset</h2><p>Your password reset code is: <b>{code}</b></p>";
        await _emailService.SendEmailAsync(user.Email, "Slush - Password Reset", emailBody);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) throw new Exception("User not found.");

        var verificationCode = await _context.VerificationCodes
            .FirstOrDefaultAsync(vc => vc.UserId == user.Id && vc.Code == request.Code && vc.Purpose == CodePurpose.PasswordReset);

        if (verificationCode == null) throw new Exception("Invalid reset code.");
        if (verificationCode.ExpiresAt < DateTime.UtcNow) throw new Exception("Reset code has expired.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        _context.VerificationCodes.Remove(verificationCode);

        await _context.SaveChangesAsync();
    }

    public async Task ResendVerificationCodeAsync(ResendVerificationCodeDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null) return;

        if (user.IsEmailVerified)
        {
            throw new Exception("Email is already verified.");
        }

        var existingCodes = await _context.VerificationCodes
            .Where(vc => vc.UserId == user.Id && vc.Purpose == CodePurpose.EmailVerification)
            .ToListAsync();

        if (existingCodes.Any())
        {
            _context.VerificationCodes.RemoveRange(existingCodes);
        }

        string newCode = new Random().Next(10000, 99999).ToString();

        _context.VerificationCodes.Add(new VerificationCode
        {
            UserId = user.Id,
            Code = newCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Purpose = CodePurpose.EmailVerification
        });

        await _context.SaveChangesAsync();

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
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpiryMinutes"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}