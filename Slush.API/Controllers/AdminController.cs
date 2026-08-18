using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Admin;
using Slush.Infrastructure.Data;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? searchTerm = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerTerm = searchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(lowerTerm) ||
                u.Email.ToLower().Contains(lowerTerm));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListDto(
                u.Id,
                u.Username,
                u.Email,
                u.IsBanned,
                u.Role.ToString(),
                u.CreatedAt))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users/{userId}/toggle-ban")]
    public async Task<IActionResult> ToggleBan(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        if (user.Email == User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value)
        {
            return BadRequest(new { message = "You cannot ban yourself." });
        }

        user.IsBanned = !user.IsBanned;
        await _context.SaveChangesAsync();

        string status = user.IsBanned ? "banned" : "unbanned";
        return Ok(new { message = $"User {user.Username} has been {status}." });
    }
}