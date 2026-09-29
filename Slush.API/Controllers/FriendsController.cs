using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.Interfaces;
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
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetFriends([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _friendService.GetFriendsAsync(GetUserId(), page, pageSize);
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

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Query cannot be empty." });

        var result = await _friendService.SearchUsersAsync(GetUserId(), query, page, pageSize);
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("requests/{requestId}/accept")]
    public async Task<IActionResult> AcceptRequest(Guid requestId)
    {
        try
        {
            await _friendService.AcceptRequestAsync(GetUserId(), requestId);
            return Ok(new { message = "Friend request accepted." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("requests/{requestId}/reject")]
    public async Task<IActionResult> RejectRequest(Guid requestId)
    {
        try
        {
            await _friendService.RejectRequestAsync(GetUserId(), requestId);
            return Ok(new { message = "Friend request rejected." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("requests/{requestId}")]
    public async Task<IActionResult> CancelRequest(Guid requestId)
    {
        try
        {
            await _friendService.CancelRequestAsync(GetUserId(), requestId);
            return Ok(new { message = "Friend request cancelled." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{friendUserId}")]
    public async Task<IActionResult> RemoveFriend(Guid friendUserId)
    {
        try
        {
            await _friendService.RemoveFriendAsync(GetUserId(), friendUserId);
            return Ok(new { message = "Friend removed." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}