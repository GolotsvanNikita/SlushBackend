using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediaUploadService _mediaService;
    private readonly IUnitOfWork _uow;

    public MediaController(IMediaUploadService mediaService, IUnitOfWork uow)
    {
        _mediaService = mediaService;
        _uow = uow;
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        var avatarUrl = await _mediaService.UploadAvatarAsync(file);
        if (avatarUrl == null) return BadRequest(new { message = "Failed to upload avatar." });

        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user != null)
        {
            user.AvatarUrl = avatarUrl;
            await _uow.SaveChangesAsync();
        }

        return Ok(new { url = avatarUrl });
    }

    [HttpPost("banner")]
    public async Task<IActionResult> UploadBanner(IFormFile file)
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        var bannerUrl = await _mediaService.UploadBannerAsync(file);
        if (bannerUrl == null) return BadRequest(new { message = "Failed to upload banner." });

        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user != null)
        {
            user.CoverUrl = bannerUrl;
            await _uow.SaveChangesAsync();
        }

        return Ok(new { url = bannerUrl });
    }

    [HttpPost("video")]
    public async Task<IActionResult> UploadVideo(IFormFile file)
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        var videoUrl = await _mediaService.UploadVideoAsync(file);
        if (videoUrl == null) return BadRequest(new { message = "Failed to upload video." });

        return Ok(new { url = videoUrl });
    }
}