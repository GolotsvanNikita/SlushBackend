using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Community;
using Slush.Application.DTOs.Library;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.Infrastructure.Services;

public class LibraryService : ILibraryService
{
    private readonly IUnitOfWork _uow;

    public LibraryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResultDto<LibraryGameDto>> GetUserLibraryAsync(Guid userId, string? sort, int page, int pageSize)
    {
        var query = _uow.Repository<UserGame>().AsQueryable()
            .Where(g => g.UserId == userId);

        query = (sort?.ToLower()) switch
        {
            "title" or "alphabetical" => query.OrderBy(g => g.Title),
            "favorites" => query.OrderByDescending(g => g.IsFavorite).ThenByDescending(g => g.AcquiredAt),
            _ => query.OrderByDescending(g => g.AcquiredAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => MapToLibraryGameDto(g))
            .ToListAsync();

        return new PagedResultDto<LibraryGameDto>(items, totalCount, page, pageSize);
    }

    public async Task<List<LibraryGameDto>> GetFavoritesAsync(Guid userId)
    {
        return await _uow.Repository<UserGame>().AsQueryable()
            .Where(g => g.UserId == userId && g.IsFavorite)
            .OrderBy(g => g.Title)
            .Select(g => MapToLibraryGameDto(g))
            .ToListAsync();
    }

    public async Task AddToFavoritesAsync(Guid userId, string gameId)
    {
        var repo = _uow.Repository<UserGame>();
        var userGame = await repo.AsQueryable()
            .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == gameId);

        if (userGame == null)
            throw new KeyNotFoundException("Game not found in your library.");

        if (userGame.IsFavorite)
            throw new InvalidOperationException("Game is already in favorites.");

        userGame.IsFavorite = true;
        repo.Update(userGame);
        await _uow.SaveChangesAsync();
    }

    public async Task RemoveFromFavoritesAsync(Guid userId, string gameId)
    {
        var repo = _uow.Repository<UserGame>();
        var userGame = await repo.AsQueryable()
            .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == gameId);

        if (userGame == null)
            throw new KeyNotFoundException("Game not found in your library.");

        userGame.IsFavorite = false;
        repo.Update(userGame);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<LibraryCollectionDto>> GetCollectionsAsync(Guid userId)
    {
        return await _uow.Repository<LibraryCollection>().AsQueryable()
            .Include(c => c.Games)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new LibraryCollectionDto
            {
                Id = c.Id,
                Name = c.Name,
                GamesCount = c.Games.Count,
                CreatedAt = c.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            })
            .ToListAsync();
    }

    public async Task<LibraryCollectionDto> CreateCollectionAsync(Guid userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Collection name cannot be empty.");

        var trimmedName = name.Trim();
        var repo = _uow.Repository<LibraryCollection>();

        var exists = await repo.AsQueryable()
            .AnyAsync(c => c.UserId == userId && c.Name.ToLower() == trimmedName.ToLower());

        if (exists)
            throw new InvalidOperationException("Collection with this name already exists.");

        var collection = new LibraryCollection
        {
            UserId = userId,
            Name = trimmedName,
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(collection);
        await _uow.SaveChangesAsync();

        return new LibraryCollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            GamesCount = 0,
            CreatedAt = collection.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    public async Task<LibraryCollectionDto> RenameCollectionAsync(Guid userId, Guid collectionId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Collection name cannot be empty.");

        var trimmedName = newName.Trim();
        var repo = _uow.Repository<LibraryCollection>();

        var collection = await repo.AsQueryable()
            .Include(c => c.Games)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new KeyNotFoundException("Collection not found.");

        if (collection.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to edit this collection.");

        var duplicate = await repo.AsQueryable()
            .AnyAsync(c => c.UserId == userId && c.Id != collectionId && c.Name.ToLower() == trimmedName.ToLower());

        if (duplicate)
            throw new InvalidOperationException("Another collection with this name already exists.");

        collection.Name = trimmedName;
        repo.Update(collection);
        await _uow.SaveChangesAsync();

        return new LibraryCollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            GamesCount = collection.Games.Count,
            CreatedAt = collection.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    public async Task DeleteCollectionAsync(Guid userId, Guid collectionId)
    {
        var repo = _uow.Repository<LibraryCollection>();
        var collection = await repo.GetByIdAsync(collectionId);

        if (collection == null)
            throw new KeyNotFoundException("Collection not found.");

        if (collection.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to delete this collection.");

        repo.Remove(collection);
        await _uow.SaveChangesAsync();
    }

    public async Task AddGameToCollectionAsync(Guid userId, Guid collectionId, string gameId)
    {
        var collection = await _uow.Repository<LibraryCollection>().GetByIdAsync(collectionId);
        if (collection == null)
            throw new KeyNotFoundException("Collection not found.");

        if (collection.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to modify this collection.");

        var gameRepo = _uow.Repository<UserGame>();
        var userGame = await gameRepo.AsQueryable()
            .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == gameId);

        if (userGame == null)
            throw new KeyNotFoundException("Game not found in your library.");

        if (userGame.CollectionId == collectionId)
            throw new InvalidOperationException("Game is already in this collection.");

        userGame.CollectionId = collectionId;
        gameRepo.Update(userGame);
        await _uow.SaveChangesAsync();
    }

    public async Task RemoveGameFromCollectionAsync(Guid userId, Guid collectionId, string gameId)
    {
        var collection = await _uow.Repository<LibraryCollection>().GetByIdAsync(collectionId);
        if (collection == null)
            throw new KeyNotFoundException("Collection not found.");

        if (collection.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to modify this collection.");

        var gameRepo = _uow.Repository<UserGame>();
        var userGame = await gameRepo.AsQueryable()
            .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == gameId);

        if (userGame == null || userGame.CollectionId != collectionId)
            throw new KeyNotFoundException("Game not found in this collection.");

        userGame.CollectionId = null;
        gameRepo.Update(userGame);
        await _uow.SaveChangesAsync();
    }

    public async Task<List<LibraryGameDto>> GetCollectionGamesAsync(Guid userId, Guid collectionId)
    {
        var collection = await _uow.Repository<LibraryCollection>().GetByIdAsync(collectionId);
        if (collection == null)
            throw new KeyNotFoundException("Collection not found.");

        if (collection.UserId != userId)
            throw new UnauthorizedAccessException("You do not have permission to view this collection.");

        return await _uow.Repository<UserGame>().AsQueryable()
            .Where(g => g.UserId == userId && g.CollectionId == collectionId)
            .OrderBy(g => g.Title)
            .Select(g => MapToLibraryGameDto(g))
            .ToListAsync();
    }

    private static LibraryGameDto MapToLibraryGameDto(UserGame g) => new()
    {
        GameId = g.GameId,
        Title = g.Title,
        CoverImage = g.ImageUrl,
        HeaderImage = g.HeaderImage,
        Developer = g.Developer,
        ReleaseDate = g.ReleaseDate,
        DiskSize = g.DiskSize,
        IsFavorite = g.IsFavorite,
        CollectionId = g.CollectionId,
        IsInstalled = g.IsInstalled,
        AcquiredAt = g.AcquiredAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
    };

    public async Task<LibraryHomeDto> GetLibraryHomeAsync(Guid userId)
    {
        var recentGames = await GetUserLibraryAsync(userId, "recent", 1, 10);
        var favorites = await GetFavoritesAsync(userId);
        var collections = await GetCollectionsAsync(userId);

        var userGameIds = await _uow.Repository<UserGame>().AsQueryable()
            .Where(g => g.UserId == userId)
            .Select(g => g.GameId)
            .ToListAsync();

        var newsQuery = _uow.Repository<CommunityPost>().AsQueryable()
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Where(p => p.PostType == Domain.Enums.CommunityPostType.News);

        if (userGameIds.Any())
            newsQuery = newsQuery.Where(p => userGameIds.Contains(p.GameId));

        var latestNews = await newsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => MapToCommunityPostDto(p, userId))
            .ToListAsync();

        var highlights = await _uow.Repository<CommunityPost>().AsQueryable()
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Where(p => p.PostType != Domain.Enums.CommunityPostType.News && p.LikesCount > 0)
            .OrderByDescending(p => p.LikesCount)
            .Take(10)
            .Select(p => MapToCommunityPostDto(p, userId))
            .ToListAsync();

        return new LibraryHomeDto
        {
            RecentGames = recentGames.Items.ToList(),
            FavoriteGames = favorites,
            Collections = collections,
            LatestNews = latestNews,
            CommunityHighlights = highlights
        };
    }

    public async Task<PagedResultDto<CommunityPostDto>> GetLibraryFeedAsync(
        Guid userId, string source, string type, string sort, int page, int pageSize)
    {
        var query = _uow.Repository<CommunityPost>().AsQueryable()
            .Include(p => p.User)
            .Include(p => p.Likes)
            .AsQueryable();

        source = source.ToLower();
        if (source == "subscriptions")
        {
            var subGameIds = await _uow.Repository<GameSubscription>().AsQueryable()
                .Where(s => s.UserId == userId)
                .Select(s => s.GameId)
                .ToListAsync();
            query = query.Where(p => subGameIds.Contains(p.GameId));
        }
        else if (source == "library")
        {
            var libGameIds = await _uow.Repository<UserGame>().AsQueryable()
                .Where(g => g.UserId == userId)
                .Select(g => g.GameId)
                .ToListAsync();
            query = query.Where(p => libGameIds.Contains(p.GameId));
        }
        else if (source == "recommended")
        {
            var monthAgo = DateTime.UtcNow.AddMonths(-1);
            query = query.Where(p => p.CreatedAt >= monthAgo);
            sort = "rating";
        }

        type = type.ToLower();
        if (type != "all" && Enum.TryParse(type, true, out Domain.Enums.CommunityPostType parsedType))
        {
            query = query.Where(p => p.PostType == parsedType);
        }

        query = (sort.ToLower()) switch
        {
            "rating" => query.OrderByDescending(p => p.LikesCount).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToCommunityPostDto(p, userId))
            .ToListAsync();

        return new PagedResultDto<CommunityPostDto>(items, totalCount, page, pageSize);
    }

    private static CommunityPostDto MapToCommunityPostDto(CommunityPost p, Guid userId) => new()
    {
        Id = p.Id.ToString(),
        GameId = p.GameId,
        AuthorUsername = p.User.Username,
        AuthorAvatarUrl = p.User.AvatarUrl,
        PostType = p.PostType.ToString(),
        Title = p.Title,
        Content = p.Content,
        ShortDescription = p.ShortDescription,
        MediaUrl = p.MediaUrl,
        LikesCount = p.LikesCount,
        CommentsCount = p.CommentsCount,
        IsLiked = p.Likes.Any(l => l.UserId == userId),
        CreatedAt = p.CreatedAt.ToString("dd.MM.yyyy")
    };

    public async Task<GameLibraryContextDto> GetGameLibraryContextAsync(Guid userId, string gameId)
    {
        var userGame = await _uow.Repository<UserGame>().AsQueryable()
            .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == gameId);

        var isInWishlist = await _uow.Repository<WishlistItem>().AsQueryable()
            .AnyAsync(w => w.UserId == userId && w.GameId == gameId);

        var isInCart = await _uow.Repository<CartItem>().AsQueryable()
            .AnyAsync(c => c.UserId == userId && c.GameId == gameId);

        var myReview = await GetMyReviewAsync(userId, gameId);

        var friendIds = await _uow.Repository<FriendRequest>().AsQueryable()
            .Where(r => (r.SenderId == userId || r.ReceiverId == userId)
                        && r.Status == Domain.Enums.FriendRequestStatus.Accepted)
            .Select(r => r.SenderId == userId ? r.ReceiverId : r.SenderId)
            .ToListAsync();

        var friendsWhoOwn = new List<Application.DTOs.Friends.FriendUserDto>();
        var friendsWhoWant = new List<Application.DTOs.Friends.FriendUserDto>();

        if (friendIds.Any())
        {
            friendsWhoOwn = await _uow.Repository<UserGame>().AsQueryable()
                .Include(ug => ug.User)
                .Where(ug => ug.GameId == gameId && friendIds.Contains(ug.UserId))
                .Select(ug => new Application.DTOs.Friends.FriendUserDto
                {
                    UserId = ug.User.Id,
                    Username = ug.User.Username,
                    AvatarUrl = ug.User.AvatarUrl,
                    LastSeenAt = ug.User.LastSeenAt,
                    IsOnline = ug.User.LastSeenAt.HasValue &&
                               (DateTime.UtcNow - ug.User.LastSeenAt.Value) <= TimeSpan.FromMinutes(2)
                })
                .ToListAsync();

            friendsWhoWant = await _uow.Repository<WishlistItem>().AsQueryable()
                .Include(w => w.User)
                .Where(w => w.GameId == gameId && friendIds.Contains(w.UserId))
                .Select(w => new Application.DTOs.Friends.FriendUserDto
                {
                    UserId = w.User.Id,
                    Username = w.User.Username,
                    AvatarUrl = w.User.AvatarUrl,
                    LastSeenAt = w.User.LastSeenAt,
                    IsOnline = w.User.LastSeenAt.HasValue &&
                               (DateTime.UtcNow - w.User.LastSeenAt.Value) <= TimeSpan.FromMinutes(2)
                })
                .ToListAsync();
        }

        return new GameLibraryContextDto
        {
            GameId = gameId,
            IsOwned = userGame != null,
            IsFavorite = userGame?.IsFavorite ?? false,
            IsInWishlist = isInWishlist,
            IsInCart = isInCart,
            CollectionId = userGame?.CollectionId,
            DiskSize = userGame?.DiskSize,
            IsInstalled = userGame?.IsInstalled,
            MyReview = myReview,
            FriendsWhoOwn = friendsWhoOwn,
            FriendsWhoWant = friendsWhoWant
        };
    }

    public async Task<UserGameReviewDto?> GetMyReviewAsync(Guid userId, string gameId)
    {
        var review = await _uow.Repository<GameReview>().AsQueryable()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == gameId);

        if (review == null) return null;

        return new UserGameReviewDto
        {
            Id = review.Id,
            GameId = review.GameId,
            Username = review.User.Username,
            AvatarUrl = review.User.AvatarUrl,
            Score = review.Score,
            Text = review.Text,
            CreatedAt = review.CreatedAt.ToString("yyyy-MM-dd")
        };
    }

    public async Task<UserGameReviewDto> UpsertMyReviewAsync(Guid userId, string gameId, SaveGameReviewRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Review text cannot be empty.");

        if (request.Score < 1 || request.Score > 10)
            throw new ArgumentException("Score must be between 1 and 10.");

        var repo = _uow.Repository<GameReview>();
        var existing = await repo.AsQueryable()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == gameId);

        if (existing == null)
        {
            existing = new GameReview
            {
                UserId = userId,
                GameId = gameId,
                GameTitle = request.GameTitle,
                GameBannerUrl = request.GameBannerUrl,
                Score = request.Score,
                Text = request.Text.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await repo.AddAsync(existing);
        }
        else
        {
            if (existing.UserId != userId)
                throw new UnauthorizedAccessException("You can only edit your own review.");

            existing.Score = request.Score;
            existing.Text = request.Text.Trim();
            if (!string.IsNullOrWhiteSpace(request.GameTitle))
                existing.GameTitle = request.GameTitle;
            if (!string.IsNullOrWhiteSpace(request.GameBannerUrl))
                existing.GameBannerUrl = request.GameBannerUrl;

            repo.Update(existing);
        }

        await _uow.SaveChangesAsync();

        var user = await _uow.Repository<User>().GetByIdAsync(userId);

        return new UserGameReviewDto
        {
            Id = existing.Id,
            GameId = existing.GameId,
            Username = user?.Username ?? string.Empty,
            AvatarUrl = user?.AvatarUrl ?? string.Empty,
            Score = existing.Score,
            Text = existing.Text,
            CreatedAt = existing.CreatedAt.ToString("yyyy-MM-dd")
        };
    }

    public async Task DeleteMyReviewAsync(Guid userId, string gameId)
    {
        var repo = _uow.Repository<GameReview>();
        var existing = await repo.AsQueryable()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.GameId == gameId);

        if (existing == null)
            throw new KeyNotFoundException("Review not found.");

        if (existing.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own review.");

        repo.Remove(existing);
        await _uow.SaveChangesAsync();
    }
}