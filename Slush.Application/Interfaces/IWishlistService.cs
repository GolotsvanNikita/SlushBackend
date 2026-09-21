using Slush.Application.DTOs.Store;

namespace Slush.Application.Interfaces;

public interface IWishlistService
{
    Task<IEnumerable<StoreItemDto>> GetWishlistAsync(Guid userId);
    Task AddToWishlistAsync(Guid userId, AddToStoreRequestDto request);
    Task RemoveFromWishlistAsync(Guid userId, string gameId);
}