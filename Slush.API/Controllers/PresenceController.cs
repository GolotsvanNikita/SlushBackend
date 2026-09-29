using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PresenceController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public PresenceController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpPost("heartbeat")]
    [Authorize]
    public async Task<IActionResult> Heartbeat()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var userRepo = _uow.Repository<User>();
        var user = await userRepo.GetByIdAsync(userId);

        if (user == null) return NotFound();

        user.LastSeenAt = DateTime.UtcNow;
        userRepo.Update(user);
        await _uow.SaveChangesAsync();

        return Ok(new
        {
            isOnline = true,
            lastSeenAt = user.LastSeenAt
        });
    }
}