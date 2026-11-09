using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameCatalogController : ControllerBase
{
    private readonly ISteamCatalogService _steamService;
    private readonly ICatalogService _cheapSharkService;
    private readonly IUnitOfWork _uow;

    public GameCatalogController(ISteamCatalogService steamService, ICatalogService cheapSharkService, IUnitOfWork uow)
    {
        _steamService = steamService;
        _cheapSharkService = cheapSharkService;
        _uow = uow;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<UnifiedGameDto>>> GetGamesList(
            [FromQuery] string? query = null,
            [FromQuery] GameSource? source = null,
            [FromQuery] int? minDiscount = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
    {
        var result = await _cheapSharkService.SearchAsync(query ?? string.Empty, source, minDiscount, page, pageSize);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var steamResult = await _steamService.SearchSteamGamesAsync(query, 1, 15);

            var combinedItems = result.Items
                .Concat(steamResult.Items)
                .GroupBy(g => g.Id)
                .Select(g => g.First())
                .Take(pageSize)
                .ToList();

            result = new PagedResultDto<UnifiedGameDto>(
                combinedItems,
                Math.Max(result.TotalCount, combinedItems.Count),
                page,
                pageSize);
        }

        return Ok(result);
    }

    [HttpGet("{appId}")]
    public async Task<ActionResult<UnifiedGameDetailsDto>> GetGameDetails(string appId)
    {
        var gameData = await _steamService.GetGameDetailsAsync(appId);

        if (gameData == null)
            return NotFound(new { message = "Game not found in Steam catalog." });

        var reviewsRepo = _uow.Repository<GameReview>().AsQueryable();
        var gameReviews = await reviewsRepo
            .Include(r => r.User)
            .Where(r => r.GameId == appId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        if (gameReviews.Any())
        {
            gameData.AverageRating = Math.Round(gameReviews.Average(r => r.Score), 1);
            gameData.Reviews = gameReviews.Select(r => new GameReviewDto
            {
                Username = r.User.Username,
                Score = r.Score,
                Text = r.Text,
                Date = r.CreatedAt.ToString("yyyy-MM-dd")
            }).ToList();
        }

        gameData.FriendsPlaying = [];

        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out Guid userId))
        {
            gameData.IsInWishlist = await _uow.Repository<WishlistItem>().AsQueryable()
                .AnyAsync(w => w.UserId == userId && w.GameId == appId);

            gameData.IsInCart = await _uow.Repository<CartItem>().AsQueryable()
                .AnyAsync(c => c.UserId == userId && c.GameId == appId);

            var friendIds = await _uow.Repository<FriendRequest>().AsQueryable()
                .Where(r => (r.SenderId == userId || r.ReceiverId == userId)
                            && r.Status == FriendRequestStatus.Accepted)
                .Select(r => r.SenderId == userId ? r.ReceiverId : r.SenderId)
                .ToListAsync();

            if (friendIds.Any())
            {
                var staleCutoff = DateTime.UtcNow.AddMinutes(-3);

                gameData.FriendsPlaying = await _uow.Repository<User>().AsQueryable()
                    .Where(u => !u.IsDeleted
                                && friendIds.Contains(u.Id)
                                && u.CurrentGameId == appId
                                && u.LastSeenAt != null
                                && u.LastSeenAt >= staleCutoff)
                    .Select(u => new FriendPlayingDto
                    {
                        UserId = u.Id,
                        Username = u.Username,
                        AvatarUrl = u.AvatarUrl,
                        Status = $"Зараз грає в {u.CurrentGameTitle ?? gameData.Title}"
                    })
                    .ToListAsync();
            }
        }

        return Ok(gameData);
    }

    [HttpGet("game/{gameId}/news")]
    public async Task<IActionResult> GetGameNews(string gameId, [FromQuery] int count = 5)
    {
        var news = await _steamService.GetGameNewsAsync(gameId, count);
        return Ok(news);
    }
}