using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Store;
using Slush.Application.Interfaces;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out Guid userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var wishlist = await _wishlistService.GetWishlistAsync(userId);
        return Ok(wishlist);
    }

    [HttpPost]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToStoreRequestDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _wishlistService.AddToWishlistAsync(userId, request);
        return Ok(new { message = "Added to wishlist." });
    }

    [HttpDelete("{gameId}")]
    public async Task<IActionResult> RemoveFromWishlist(string gameId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _wishlistService.RemoveFromWishlistAsync(userId, gameId);
        return Ok(new { message = "Removed from wishlist." });
    }
}