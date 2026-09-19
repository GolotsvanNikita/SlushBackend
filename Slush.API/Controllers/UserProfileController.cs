using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;
using Slush.Application.Interfaces;
namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public UserProfileController(IProfileService profileService)
    {
        _profileService = profileService;
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
}