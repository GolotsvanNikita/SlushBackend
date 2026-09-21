using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using System.Security.Claims;
namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IUnitOfWork _uow;

    public UserProfileController(IProfileService profileService, IUnitOfWork uow)
    {
        _profileService = profileService;
        _uow = uow;
    }

    [HttpPut("settings")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized();

        var userRepo = _uow.Repository<User>();
        var user = await userRepo.GetByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            var isUsernameTaken = userRepo.AsQueryable().Any(u => u.Username.ToLower() == request.Username.ToLower());
            if (isUsernameTaken)
            {
                return BadRequest(new { message = "This username is already taken." });
            }

            user.Username = request.Username;
        }

        if (request.Bio != null)
        {
            user.Bio = request.Bio;
        }

        userRepo.Update(user);
        await _uow.SaveChangesAsync();

        return Ok(new { message = "Profile updated successfully.", username = user.Username, bio = user.Bio });
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(string username)
    {
        var profile = await _profileService.GetUserProfileAsync(username);

        if (profile == null)
        {
            return NotFound(new { message = $"User '{username}' not found." });
        }

        return Ok(profile);
    }

    [HttpGet("{username}/comments")]
    public async Task<ActionResult<PagedResultDto<ProfileCommentDto>>> GetComments(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfileCommentsAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/reviews")]
    public async Task<ActionResult<PagedResultDto<ProfileReviewDto>>> GetReviews(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        var result = await _profileService.GetProfileReviewsAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/guides")]
    public async Task<ActionResult<PagedResultDto<ProfileGuideDto>>> GetGuides(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        var result = await _profileService.GetProfileGuidesAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/games")]
    public async Task<ActionResult<PagedResultDto<ProfileGameDto>>> GetGames(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfileGamesAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/posts")]
    public async Task<ActionResult<PagedResultDto<ProfilePostDto>>> GetPosts(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfilePostsAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/screenshots")]
    public async Task<ActionResult<PagedResultDto<ProfileScreenshotDto>>> GetScreenshots(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfileScreenshotsAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/videos")]
    public async Task<ActionResult<PagedResultDto<ProfileVideoDto>>> GetVideos(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfileVideosAsync(username, page, pageSize);
        return Ok(result);
    }
}