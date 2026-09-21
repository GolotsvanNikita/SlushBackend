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
    public async Task<IActionResult> Heartbeat()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user == null) return NotFound();

        user.LastSeenAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();


        return Ok();
    }
}