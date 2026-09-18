using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Admin;
using Slush.Application.DTOs.Auth;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin, Admin, Moderator")]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public AdminController(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? searchTerm = null)
    {
        var query = _uow.Repository<User>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerTerm = searchTerm.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(lowerTerm) ||
                u.Email.ToLower().Contains(lowerTerm));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var usersDto = _mapper.Map<IEnumerable<UserListDto>>(users);

        return Ok(usersDto);
    }

    [HttpPost("users/{userId}/toggle-ban")]
    public async Task<IActionResult> ToggleBan(Guid userId)
    {
        var userRepo = _uow.Repository<User>();
        var user = await userRepo.GetByIdAsync(userId);

        if (user == null) return NotFound(new { message = "User not found" });

        if (user.Email == User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value)
        {
            return BadRequest(new { message = "You cannot ban yourself." });
        }

        if ((user.Role == Domain.Enums.UserRole.SuperAdmin || user.Role == Domain.Enums.UserRole.Admin)
            && !User.IsInRole("SuperAdmin"))
        {
            return StatusCode(403, new { message = "You do not have permission to ban an Administrator." });
        }

        user.IsBanned = !user.IsBanned;

        userRepo.Update(user);
        await _uow.SaveChangesAsync();

        string status = user.IsBanned ? "banned" : "unbanned";
        return Ok(new { message = $"User {user.Username} has been {status}." });
    }

    [HttpPut("users/{userId}/role")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateRole(Guid userId, [FromBody] UpdateUserRoleRequestDto request)
    {
        var userRepo = _uow.Repository<User>();
        var user = await userRepo.GetByIdAsync(userId);

        if (user == null) return NotFound(new { message = "User not found." });

        if (user.Email == User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value)
        {
            return BadRequest(new { message = "You cannot change your own role." });
        }

        user.Role = request.NewRole;

        userRepo.Update(user);
        await _uow.SaveChangesAsync();

        return Ok(new { message = $"User role updated to {request.NewRole}" });
    }
}