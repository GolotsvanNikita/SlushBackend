using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;
using Slush.Infrastructure.Services;
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
            [FromQuery] bool? isFree = null,
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
        else if (isFree == true || minDiscount == 100 || page == 1)
        {
            var freeSteamGames = new List<UnifiedGameDto>
            {
                new(
                    "730",
                    "Counter-Strike 2",
                    GameSource.Steam,
                    0m,
                    0m,
                    0,
                    "https://cdn.akamai.steamstatic.com/steam/apps/730/header.jpg",
                    "https://store.steampowered.com/app/730",
                    "FPS, Shooter, Multiplayer"
                ),
                new(
                    "570",
                    "Dota 2",
                    GameSource.Steam,
                    0m,
                    0m,
                    0,
                    "https://cdn.akamai.steamstatic.com/steam/apps/570/header.jpg",
                    "https://store.steampowered.com/app/570",
                    "MOBA, Multiplayer, Strategy"
                ),
                new(
                    "578080",
                    "PUBG: BATTLEGROUNDS",
                    GameSource.Steam,
                    0m,
                    0m,
                    0,
                    "https://cdn.akamai.steamstatic.com/steam/apps/578080/header.jpg",
                    "https://store.steampowered.com/app/578080",
                    "Battle Royale, Shooter, Survival"
                ),
                new(
                    "1172470",
                    "Apex Legends",
                    GameSource.Steam,
                    0m,
                    0m,
                    0,
                    "https://cdn.akamai.steamstatic.com/steam/apps/1172470/header.jpg",
                    "https://store.steampowered.com/app/1172470",
                    "Battle Royale, FPS, Multiplayer"
                )
            };

            var items = isFree == true
                ? freeSteamGames.Concat(result.Items.Where(g => g.Price == 0)).GroupBy(g => g.Id).Select(g => g.First()).Take(pageSize).ToList()
                : result.Items.Concat(freeSteamGames).GroupBy(g => g.Id).Select(g => g.First()).Take(pageSize).ToList();

            result = new PagedResultDto<UnifiedGameDto>(
                items,
                Math.Max(result.TotalCount, items.Count),
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
                Id = r.Id,
                UserId = r.UserId,
                Username = r.User.Username,
                AvatarUrl = r.User.AvatarUrl ?? string.Empty,
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

    [HttpGet("news")]
    public async Task<ActionResult<PagedResultDto<SteamNewsItemDto>>> GetAggregatedNews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var result = await _steamService.GetAggregatedNewsAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost("{appId}/reviews")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<GameReviewDto>> UpsertGameReview(
    string appId,
    [FromBody] CreateCatalogReviewDto request)
    {
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest(new { message = "Текст рецензії не може бути порожнім." });

        var score = Math.Clamp(request.Score, 1, 10);

        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return Unauthorized();

        var userGame = await _uow.Repository<UserGame>().AsQueryable()
            .FirstOrDefaultAsync(ug => ug.UserId == userId && ug.GameId == appId);

        string gameTitle = userGame?.Title ?? string.Empty;
        string gameBanner = userGame?.HeaderImage ?? userGame?.ImageUrl ?? $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/header.jpg";

        if (string.IsNullOrWhiteSpace(gameTitle))
        {
            var steamDetails = await _steamService.GetGameDetailsAsync(appId);
            gameTitle = steamDetails?.Title ?? $"Game {appId}";
        }

        var reviewRepo = _uow.Repository<GameReview>();
        var existingReview = await reviewRepo.AsQueryable()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == appId);

        if (existingReview != null)
        {
            existingReview.Score = score;
            existingReview.Text = request.Text.Trim();
            existingReview.GameTitle = gameTitle;
            existingReview.GameBannerUrl = gameBanner;
            existingReview.CreatedAt = DateTime.UtcNow;
            reviewRepo.Update(existingReview);
        }
        else
        {
            existingReview = new GameReview
            {
                UserId = userId,
                GameId = appId,
                GameTitle = gameTitle,
                GameBannerUrl = gameBanner,
                Score = score,
                Text = request.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await reviewRepo.AddAsync(existingReview);
        }

        await _uow.SaveChangesAsync();

        return Ok(new GameReviewDto
        {
            Id = existingReview.Id,
            UserId = user.Id,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            Score = existingReview.Score,
            Text = existingReview.Text,
            Date = existingReview.CreatedAt.ToString("yyyy-MM-dd")
        });
    }

    [HttpDelete("{appId}/reviews")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> DeleteGameReview(string appId)
    {
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out Guid userId))
            return Unauthorized();

        var reviewRepo = _uow.Repository<GameReview>();
        var existingReview = await reviewRepo.AsQueryable()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == appId);

        if (existingReview == null)
            return NotFound(new { message = "Рецензію не знайдено." });

        reviewRepo.Remove(existingReview);
        await _uow.SaveChangesAsync();

        return Ok(new { message = "Рецензію видалено." });
    }
}