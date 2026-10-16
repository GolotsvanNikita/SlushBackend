using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Community;
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

    Task<LibraryHomeDto> GetLibraryHomeAsync(Guid userId);
    Task<PagedResultDto<CommunityPostDto>> GetLibraryFeedAsync(
        Guid userId,
        string source,
        string type,
        string sort,
        int page,
        int pageSize);

    Task<GameLibraryContextDto> GetGameLibraryContextAsync(Guid userId, string gameId);

    Task<UserGameReviewDto?> GetMyReviewAsync(Guid userId, string gameId);
    Task<UserGameReviewDto> UpsertMyReviewAsync(Guid userId, string gameId, SaveGameReviewRequestDto request);
    Task DeleteMyReviewAsync(Guid userId, string gameId);

    Task<int> CheckoutCartAsync(Guid userId);
}