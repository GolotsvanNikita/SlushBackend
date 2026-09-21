using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Store;
using Slush.Application.Interfaces;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out Guid userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var cart = await _cartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToStoreRequestDto request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _cartService.AddToCartAsync(userId, request);
        return Ok(new { message = "Added to cart." });
    }

    [HttpDelete("{gameId}")]
    public async Task<IActionResult> RemoveFromCart(string gameId)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _cartService.RemoveFromCartAsync(userId, gameId);
        return Ok(new { message = "Removed from cart." });
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _cartService.ClearCartAsync(userId);
        return Ok(new { message = "Cart cleared." });
    }
}