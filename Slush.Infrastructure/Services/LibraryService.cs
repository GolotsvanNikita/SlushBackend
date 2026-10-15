using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Common;
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
}