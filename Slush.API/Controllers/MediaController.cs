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

    public MediaController(
        IMediaUploadService mediaService,
        IUnitOfWork uow)
    {
        _mediaService = mediaService;
        _uow = uow;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdString =
            User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdString, out userId);
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (!TryGetCurrentUserId(out Guid userId))
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Avatar file is required." });

        var avatarUrl = await _mediaService.UploadAvatarAsync(file);

        if (avatarUrl == null)
            return BadRequest(new { message = "Failed to upload avatar." });

        var user = await _uow.Repository<User>().GetByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        user.AvatarUrl = avatarUrl;

        await _uow.SaveChangesAsync();

        return Ok(new { url = avatarUrl });
    }

    [HttpPost("banner")]
    public async Task<IActionResult> UploadBanner(IFormFile file)
    {
        if (!TryGetCurrentUserId(out Guid userId))
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Banner file is required." });

        var bannerUrl = await _mediaService.UploadBannerAsync(file);

        if (bannerUrl == null)
            return BadRequest(new { message = "Failed to upload banner." });

        var user = await _uow.Repository<User>().GetByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        user.CoverUrl = bannerUrl;

        await _uow.SaveChangesAsync();

        return Ok(new { url = bannerUrl });
    }

    [HttpPost("video")]
    public async Task<IActionResult> UploadVideo(
        IFormFile file,
        [FromForm] string gameId,
        [FromForm] string gameTitle,
        [FromForm] string title)
    {
        if (!TryGetCurrentUserId(out Guid userId))
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Video file is required." });

        if (string.IsNullOrWhiteSpace(gameId))
            return BadRequest(new { message = "Game ID is required." });

        if (string.IsNullOrWhiteSpace(gameTitle))
            return BadRequest(new { message = "Game title is required." });

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new { message = "Video title is required." });

        var videoUrl = await _mediaService.UploadVideoAsync(file);

        if (videoUrl == null)
            return BadRequest(new { message = "Failed to upload video." });

        var video = new UserVideo
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            GameTitle = gameTitle,
            Title = title,
            VideoUrl = videoUrl,
            ThumbnailUrl = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Repository<UserVideo>().AddAsync(video);
        await _uow.SaveChangesAsync();

        return Ok(new
        {
            id = video.Id,
            userId = video.UserId,
            gameId = video.GameId,
            gameTitle = video.GameTitle,
            title = video.Title,
            videoUrl = video.VideoUrl,
            thumbnailUrl = video.ThumbnailUrl,
            createdAt = video.CreatedAt
        });
    }

    [HttpPost("screenshot")]
    public async Task<IActionResult> UploadScreenshot(IFormFile file)
    {
        if (!TryGetCurrentUserId(out Guid userId))
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Screenshot file is required." });

        var screenshotUrl = await _mediaService.UploadScreenshotAsync(file);

        if (screenshotUrl == null)
            return BadRequest(new { message = "Failed to upload screenshot." });

        return Ok(new { url = screenshotUrl });
    }
}
