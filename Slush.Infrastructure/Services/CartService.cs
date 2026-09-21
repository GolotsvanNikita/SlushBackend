using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Store;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;

    public CartService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<StoreItemDto>> GetCartAsync(Guid userId)
    {
        return await _uow.Repository<CartItem>().AsQueryable()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.AddedAt)
            .Select(c => new StoreItemDto
            {
                GameId = c.GameId,
                Title = c.Title,
                ImageUrl = c.ImageUrl,
                Price = c.Price,
                AddedAt = c.AddedAt
            })
            .ToListAsync();
    }

    public async Task AddToCartAsync(Guid userId, AddToStoreRequestDto request)
    {
        var repo = _uow.Repository<CartItem>();

        bool exists = await repo.AsQueryable()
            .AnyAsync(c => c.UserId == userId && c.GameId == request.GameId);

        if (exists) return;

        var item = new CartItem
        {
            UserId = userId,
            GameId = request.GameId,
            Title = request.Title,
            ImageUrl = request.ImageUrl,
            Price = request.Price
        };

        await repo.AddAsync(item);
        await _uow.SaveChangesAsync();
    }

    public async Task RemoveFromCartAsync(Guid userId, string gameId)
    {
        var repo = _uow.Repository<CartItem>();
        var item = await repo.AsQueryable()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.GameId == gameId);

        if (item != null)
        {
            repo.Remove(item);
            await _uow.SaveChangesAsync();
        }
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var repo = _uow.Repository<CartItem>();
        var items = await repo.AsQueryable().Where(c => c.UserId == userId).ToListAsync();

        if (items.Any())
        {
            foreach (var item in items) repo.Remove(item);
            await _uow.SaveChangesAsync();
        }
    }
}