using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Store;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.Infrastructure.Services;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _uow;

    public WishlistService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<StoreItemDto>> GetWishlistAsync(Guid userId)
    {
        return await _uow.Repository<WishlistItem>().AsQueryable()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .Select(w => new StoreItemDto
            {
                GameId = w.GameId,
                Title = w.Title,
                ImageUrl = w.ImageUrl,
                Price = w.Price,
                OldPrice = w.OldPrice,
                DiscountPercent = w.DiscountPercent,
                AddedAt = w.AddedAt
            })
            .ToListAsync();
    }

    public async Task AddToWishlistAsync(Guid userId, AddToStoreRequestDto request)
    {
        var repo = _uow.Repository<WishlistItem>();

        bool exists = await repo.AsQueryable()
            .AnyAsync(w => w.UserId == userId && w.GameId == request.GameId);

        if (exists) return;

        var item = new WishlistItem
        {
            UserId = userId,
            GameId = request.GameId,
            Title = request.Title,
            ImageUrl = request.ImageUrl,
            Price = request.Price,
            OldPrice = request.OldPrice,
            DiscountPercent = request.DiscountPercent
        };

        await repo.AddAsync(item);
        await _uow.SaveChangesAsync();
    }

    public async Task RemoveFromWishlistAsync(Guid userId, string gameId)
    {
        var repo = _uow.Repository<WishlistItem>();
        var item = await repo.AsQueryable()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.GameId == gameId);

        if (item != null)
        {
            repo.Remove(item);
            await _uow.SaveChangesAsync();
        }
    }
}