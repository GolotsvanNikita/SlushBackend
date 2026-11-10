using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Store;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;
    private readonly IUnitOfWork _uow;

    public WishlistController(
        IWishlistService wishlistService,
        IUnitOfWork uow)
    {
        _wishlistService = wishlistService;
        _uow = uow;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var wishlist = await _wishlistService.GetWishlistAsync(GetUserId());
        return Ok(wishlist);
    }

    [AllowAnonymous]
    [HttpGet("user/{username}")]
    public async Task<IActionResult> GetUserWishlist(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Username is required." });

        var normalizedUsername = username.Trim().ToLower();

        var user = await _uow.Repository<User>().AsQueryable()
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Username.ToLower() == normalizedUsername);

        if (user == null)
            return NotFound(new { message = "User not found." });

        var wishlist = await _wishlistService.GetWishlistAsync(user.Id);

        return Ok(wishlist);
    }

    [HttpPost]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToStoreRequestDto request)
    {
        await _wishlistService.AddToWishlistAsync(GetUserId(), request);
        return Ok(new { message = "Added to wishlist." });
    }

    [HttpDelete("{gameId}")]
    public async Task<IActionResult> RemoveFromWishlist(string gameId)
    {
        await _wishlistService.RemoveFromWishlistAsync(GetUserId(), gameId);
        return Ok(new { message = "Removed from wishlist." });
    }
}