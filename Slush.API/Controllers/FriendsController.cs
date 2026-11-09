using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly IFriendService _friendService;

    public FriendsController(IFriendService friendService)
    {
        _friendService = friendService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetFriends(
        [FromQuery] string? filter = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _friendService.GetFriendsAsync(GetUserId(), filter, page, pageSize);
        return Ok(result);
    }

    [HttpGet("requests/incoming")]
    public async Task<IActionResult> GetIncomingRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _friendService.GetIncomingRequestsAsync(GetUserId(), page, pageSize);
        return Ok(result);
    }

    [HttpGet("requests/outgoing")]
    public async Task<IActionResult> GetOutgoingRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _friendService.GetOutgoingRequestsAsync(GetUserId(), page, pageSize);
        return Ok(result);
    }

    [HttpGet("status/{targetUserId}")]
    public async Task<IActionResult> GetStatus(Guid targetUserId)
    {
        var result = await _friendService.GetStatusAsync(GetUserId(), targetUserId);
        return Ok(result);
    }

    [HttpPost("request/{targetUserId}")]
    public async Task<IActionResult> SendRequest(Guid targetUserId)
    {
        try
        {
            await _friendService.SendRequestAsync(GetUserId(), targetUserId);
            return Ok(new { message = "Friend request sent." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("requests/{requestId}/accept")]
    public async Task<IActionResult> AcceptRequest(Guid requestId)
    {
        try
        {
            await _friendService.AcceptRequestAsync(GetUserId(), requestId);
            return Ok(new { message = "Friend request accepted." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("requests/{requestId}/reject")]
    public async Task<IActionResult> RejectRequest(Guid requestId)
    {
        try
        {
            await _friendService.RejectRequestAsync(GetUserId(), requestId);
            return Ok(new { message = "Friend request rejected." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("requests/{requestId}")]
    public async Task<IActionResult> CancelRequest(Guid requestId)
    {
        try
        {
            await _friendService.CancelRequestAsync(GetUserId(), requestId);
            return Ok(new { message = "Friend request cancelled." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{friendUserId}")]
    public async Task<IActionResult> RemoveFriend(Guid friendUserId)
    {
        try
        {
            await _friendService.RemoveFriendAsync(GetUserId(), friendUserId);
            return Ok(new { message = "Friend removed." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? query = null,
        [FromQuery] string? gameId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _friendService.SearchUsersAsync(GetUserId(), query, gameId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("blocked")]
    public async Task<IActionResult> GetBlockedUsers()
    {
        var result = await _friendService.GetBlockedUsersAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("{userId}/block")]
    public async Task<IActionResult> BlockUser(Guid userId)
    {
        try
        {
            await _friendService.BlockUserAsync(GetUserId(), userId);
            return Ok(new { message = "User blocked." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{userId}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid userId)
    {
        try
        {
            await _friendService.UnblockUserAsync(GetUserId(), userId);
            return Ok(new { message = "User unblocked." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _friendService.GetFriendsActivityAsync(GetUserId(), page, pageSize);
        return Ok(result);
    }
}