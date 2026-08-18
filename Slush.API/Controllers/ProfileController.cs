using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMyProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var username = User.FindFirst("unique_name")?.Value;

        return Ok(new
        {
            Message = "Access confirm.",
            UserId = userId,
            Email = email,
            Username = username
        });
    }
}