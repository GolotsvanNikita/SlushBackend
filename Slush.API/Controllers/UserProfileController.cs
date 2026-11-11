using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;
using Slush.Application.DTOs.Store;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
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

    private Guid? GetViewerUserIdOrNull()
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }

    [HttpPut("settings")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
    {
        var userId = GetViewerUserIdOrNull();
        if (!userId.HasValue)
            return Unauthorized();

        var userRepo = _uow.Repository<User>();
        var user = await userRepo.GetByIdAsync(userId.Value);

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

    [HttpGet("{username}/wishlist")]
    public async Task<ActionResult<PagedResultDto<StoreItemDto>>> GetWishlist(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var result = await _profileService.GetProfileWishlistAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/comments")]
    public async Task<ActionResult<PagedResultDto<ProfileCommentDto>>> GetComments(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfileCommentsAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/reviews")]
    public async Task<ActionResult<PagedResultDto<ProfileReviewDto>>> GetReviews(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5)
    {
        var result = await _profileService.GetProfileReviewsAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/guides")]
    public async Task<ActionResult<PagedResultDto<ProfileGuideDto>>> GetGuides(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5)
    {
        var result = await _profileService.GetProfileGuidesAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/games")]
    public async Task<ActionResult<PagedResultDto<ProfileGameDto>>> GetGames(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfileGamesAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/posts")]
    public async Task<ActionResult<PagedResultDto<ProfilePostDto>>> GetPosts(
        string username,
        [FromQuery] string? type = "Discussion",
        [FromQuery] string? sort = "Newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfilePostsAsync(
            username, GetViewerUserIdOrNull(), type, sort, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/screenshots")]
    public async Task<ActionResult<PagedResultDto<ProfileScreenshotDto>>> GetScreenshots(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfileScreenshotsAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{username}/videos")]
    public async Task<ActionResult<PagedResultDto<ProfileVideoDto>>> GetVideos(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _profileService.GetProfileVideosAsync(username, page, pageSize);
        return Ok(result);
    }

    [HttpPost("{username}/comments")]
    [Authorize]
    public async Task<ActionResult<ProfileCommentDto>> AddComment(
    string username,
    [FromBody] CreateProfileCommentDto request)
    {
        var userId = GetViewerUserIdOrNull();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var comment = await _profileService.AddProfileCommentAsync(userId.Value, username, request.Text);
            return Ok(comment);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("comments/{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var userId = GetViewerUserIdOrNull();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            await _profileService.DeleteProfileCommentAsync(userId.Value, commentId);
            return Ok(new { message = "Comment deleted successfully." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }
}