using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Profile;
using Slush.Application.Interfaces;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out Guid userId) ? userId : Guid.Empty;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfileSettings()
    {
        var profile = await _settingsService.GetProfileSettingsAsync(GetUserId());
        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfileSettings([FromBody] UpdateProfileSettingsDto request)
    {
        try
        {
            await _settingsService.UpdateProfileSettingsAsync(GetUserId(), request);
            return Ok(new { message = "Profile updated successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationSettings()
    {
        var settings = await _settingsService.GetNotificationSettingsAsync(GetUserId());
        return Ok(settings);
    }

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] NotificationSettingsDto request)
    {
        await _settingsService.UpdateNotificationSettingsAsync(GetUserId(), request);
        return Ok(new { message = "Notification settings updated." });
    }
}