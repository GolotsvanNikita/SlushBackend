using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Auth;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Infrastructure.Data;

namespace Slush.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegisterAsync(RegisterDto request)
    {
        bool userExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email || u.Username == request.Username);

        if (userExists)
        {
            throw new Exception("User with this username or email already exists.");
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            IsEmailVerified = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // TODO: Генерация 5-значного кода и отправка на email
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.LoginOrEmail || u.Username == request.LoginOrEmail);

        if (user == null)
        {
            throw new Exception("Invalid username or password.");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new Exception("Invalid username or password.");
        }

        if (!user.IsEmailVerified)
        {
            throw new Exception("Please verify your email address.");
        }

        return new AuthResponseDto("fake-jwt-token", "fake-refresh-token");
    }

    public Task VerifyEmailAsync(VerifyCodeDto request) => throw new NotImplementedException();
    public Task ForgotPasswordAsync(ForgotPasswordDto request) => throw new NotImplementedException();
    public Task ResetPasswordAsync(ResetPasswordDto request) => throw new NotImplementedException();
}