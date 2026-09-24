using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Community;
using Slush.Application.Interfaces;
using Slush.Domain.Enums;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _communityService;

    public CommunityController(ICommunityService communityService)
    {
        _communityService = communityService;
    }

    [HttpGet("game/{gameId}")]
    public async Task<IActionResult> GetPosts(
        string gameId,
        [FromQuery] CommunityPostType? type = null,
        [FromQuery] PostSortOption sort = PostSortOption.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var currentUserId = GetUserId();
        var userIdOrNull = currentUserId == Guid.Empty ? (Guid?)null : currentUserId;

        var result = await _communityService.GetGamePostsAsync(gameId, type, sort, page, pageSize, userIdOrNull);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePost([FromBody] CreateCommunityPostDto request)
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.GameId))
            return BadRequest(new { message = "GameId is required." });

        var createdPost = await _communityService.CreatePostAsync(userId, request);

        return Ok(createdPost);
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out Guid userId) ? userId : Guid.Empty;
    }


    [HttpPost("{postId}/like")]
    [Authorize]
    public async Task<IActionResult> ToggleLike(Guid postId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _communityService.ToggleLikeAsync(userId, postId);
        return Ok(new { message = "Like toggled successfully." });
    }

    [HttpPost("{postId}/comment")]
    [Authorize]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] CreateCommentDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "Comment content cannot be empty." });

        var comment = await _communityService.AddCommentAsync(userId, postId, request);
        return Ok(comment);
    }

    [HttpGet("{postId}/comments")]
    public async Task<IActionResult> GetComments(Guid postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _communityService.GetPostCommentsAsync(postId, page, pageSize);
        return Ok(result);
    }

    [HttpPut("{postId}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(Guid postId, [FromBody] UpdateCommunityPostDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            await _communityService.UpdatePostAsync(userId, postId, request);
            return Ok(new { message = "Post updated successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpDelete("{postId}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(Guid postId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            await _communityService.DeletePostAsync(userId, postId);
            return Ok(new { message = "Post deleted successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }


    [HttpPut("comment/{commentId}")]
    [Authorize]
    public async Task<IActionResult> UpdateComment(Guid commentId, [FromBody] UpdateCommentDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "Comment content cannot be empty." });

        try
        {
            await _communityService.UpdateCommentAsync(userId, commentId, request);
            return Ok(new { message = "Comment updated successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpDelete("comment/{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            await _communityService.DeleteCommentAsync(userId, commentId);
            return Ok(new { message = "Comment deleted successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpGet("game/{gameId}/counts")]
    public async Task<IActionResult> GetTabCounts(string gameId)
    {
        var currentUserId = GetUserId();

        var userIdOrNull = currentUserId == Guid.Empty
            ? (Guid?)null
            : currentUserId;

        var result = await _communityService.GetGameTabCountsAsync(
            gameId,
            userIdOrNull);

        return Ok(result);
    }

    [HttpPost("game/{gameId}/subscribe")]
    [Authorize]
    public async Task<IActionResult> ToggleSubscribe(string gameId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _communityService.ToggleSubscribeAsync(userId, gameId);
        return Ok(new { message = "Subscription toggled successfully." });
    }
}