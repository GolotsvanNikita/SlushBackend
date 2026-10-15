using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Library;

namespace Slush.Application.Interfaces;

public interface ILibraryService
{
    Task<PagedResultDto<LibraryGameDto>> GetUserLibraryAsync(Guid userId, string? sort, int page, int pageSize);
    Task<List<LibraryGameDto>> GetFavoritesAsync(Guid userId);
    Task AddToFavoritesAsync(Guid userId, string gameId);
    Task RemoveFromFavoritesAsync(Guid userId, string gameId);

    Task<List<LibraryCollectionDto>> GetCollectionsAsync(Guid userId);
    Task<LibraryCollectionDto> CreateCollectionAsync(Guid userId, string name);
    Task<LibraryCollectionDto> RenameCollectionAsync(Guid userId, Guid collectionId, string newName);
    Task DeleteCollectionAsync(Guid userId, Guid collectionId);

    Task AddGameToCollectionAsync(Guid userId, Guid collectionId, string gameId);
    Task RemoveGameFromCollectionAsync(Guid userId, Guid collectionId, string gameId);
    Task<List<LibraryGameDto>> GetCollectionGamesAsync(Guid userId, Guid collectionId);
}