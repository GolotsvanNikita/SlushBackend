using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Library;
using Slush.Application.Interfaces;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LibraryController : ControllerBase
{
    private readonly ILibraryService _libraryService;

    public LibraryController(ILibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetLibrary(
        [FromQuery] string? sort = "recent",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _libraryService.GetUserLibraryAsync(GetUserId(), sort, page, pageSize);
        return Ok(result);
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        var result = await _libraryService.GetFavoritesAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("{gameId}/favorite")]
    public async Task<IActionResult> AddToFavorites(string gameId)
    {
        try
        {
            await _libraryService.AddToFavoritesAsync(GetUserId(), gameId);
            return Ok(new { message = "Game added to favorites." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{gameId}/favorite")]
    public async Task<IActionResult> RemoveFromFavorites(string gameId)
    {
        try
        {
            await _libraryService.RemoveFromFavoritesAsync(GetUserId(), gameId);
            return Ok(new { message = "Game removed from favorites." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("collections")]
    public async Task<IActionResult> GetCollections()
    {
        var result = await _libraryService.GetCollectionsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("collections")]
    public async Task<IActionResult> CreateCollection([FromBody] SaveCollectionRequestDto request)
    {
        try
        {
            var result = await _libraryService.CreateCollectionAsync(GetUserId(), request.Name);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("collections/{collectionId}")]
    public async Task<IActionResult> RenameCollection(Guid collectionId, [FromBody] SaveCollectionRequestDto request)
    {
        try
        {
            var result = await _libraryService.RenameCollectionAsync(GetUserId(), collectionId, request.Name);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("collections/{collectionId}")]
    public async Task<IActionResult> DeleteCollection(Guid collectionId)
    {
        try
        {
            await _libraryService.DeleteCollectionAsync(GetUserId(), collectionId);
            return Ok(new { message = "Collection deleted." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost("collections/{collectionId}/games/{gameId}")]
    public async Task<IActionResult> AddGameToCollection(Guid collectionId, string gameId)
    {
        try
        {
            await _libraryService.AddGameToCollectionAsync(GetUserId(), collectionId, gameId);
            return Ok(new { message = "Game added to collection." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("collections/{collectionId}/games/{gameId}")]
    public async Task<IActionResult> RemoveGameFromCollection(Guid collectionId, string gameId)
    {
        try
        {
            await _libraryService.RemoveGameFromCollectionAsync(GetUserId(), collectionId, gameId);
            return Ok(new { message = "Game removed from collection." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("collections/{collectionId}/games")]
    public async Task<IActionResult> GetCollectionGames(Guid collectionId)
    {
        try
        {
            var result = await _libraryService.GetCollectionGamesAsync(GetUserId(), collectionId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpGet("home")]
    public async Task<IActionResult> GetLibraryHome()
    {
        var result = await _libraryService.GetLibraryHomeAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("feed")]
    public async Task<IActionResult> GetLibraryFeed(
        [FromQuery] string source = "library",
        [FromQuery] string type = "all",
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _libraryService.GetLibraryFeedAsync(GetUserId(), source, type, sort, page, pageSize);
        return Ok(result);
    }

    [HttpGet("game/{gameId}")]
    public async Task<IActionResult> GetGameLibraryContext(string gameId)
    {
        var result = await _libraryService.GetGameLibraryContextAsync(GetUserId(), gameId);
        return Ok(result);
    }

    [HttpGet("game/{gameId}/review")]
    public async Task<IActionResult> GetMyReview(string gameId)
    {
        var review = await _libraryService.GetMyReviewAsync(GetUserId(), gameId);
        if (review == null) return NotFound(new { message = "Review not found." });
        return Ok(review);
    }

    [HttpPost("game/{gameId}/review")]
    [HttpPut("game/{gameId}/review")]
    public async Task<IActionResult> SaveMyReview(string gameId, [FromBody] SaveGameReviewRequestDto request)
    {
        try
        {
            var result = await _libraryService.UpsertMyReviewAsync(GetUserId(), gameId, request);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpDelete("game/{gameId}/review")]
    public async Task<IActionResult> DeleteMyReview(string gameId)
    {
        try
        {
            await _libraryService.DeleteMyReviewAsync(GetUserId(), gameId);
            return Ok(new { message = "Review deleted." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutCart()
    {
        try
        {
            var count = await _libraryService.CheckoutCartAsync(GetUserId());
            return Ok(new { message = $"Successfully added {count} game(s) to your library.", addedCount = count });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}