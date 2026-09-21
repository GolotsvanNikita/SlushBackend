using Slush.Application.DTOs.Store;

namespace Slush.Application.Interfaces;

public interface ICartService
{
    Task<IEnumerable<StoreItemDto>> GetCartAsync(Guid userId);
    Task AddToCartAsync(Guid userId, AddToStoreRequestDto request);
    Task RemoveFromCartAsync(Guid userId, string gameId);
    Task ClearCartAsync(Guid userId);
}