using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Purchases;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;

namespace Slush.Infrastructure.Services;

public class PurchaseService : IPurchaseService
{
    private const decimal UsdToUahRate = 45m;

    private readonly IUnitOfWork _uow;
    private readonly ISteamCatalogService _steamService;

    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _userLocks = new();

    public PurchaseService(IUnitOfWork uow, ISteamCatalogService steamService)
    {
        _uow = uow;
        _steamService = steamService;
    }

    public async Task<PurchaseResultDto> PurchaseGameAsync(Guid userId, string gameId)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Invalid game ID.");

        var userLock = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync();

        try
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
                throw new UnauthorizedAccessException("User not found.");

            var userGameRepo = _uow.Repository<UserGame>();
            bool alreadyOwned = await userGameRepo.AsQueryable()
                .AnyAsync(ug => ug.UserId == userId && ug.GameId == gameId);

            if (alreadyOwned)
                throw new InvalidOperationException("You already own this game.");

            var gameDetails = await _steamService.GetGameDetailsAsync(gameId);
            if (gameDetails == null)
                throw new KeyNotFoundException($"Game with ID '{gameId}' was not found in Steam catalog.");

            decimal priceUsd = ExtractValidatedPrice(gameDetails.Price, gameDetails.IsFree);
            decimal actualPrice = ConvertUsdToUah(priceUsd);

            if (user.Balance < actualPrice)
                throw new InvalidOperationException(
                    $"Недостатньо коштів. Ціна гри: {actualPrice:N2} ₴, баланс: {user.Balance:N2} ₴.");

            await _uow.BeginTransactionAsync();
            try
            {
                user = await userRepo.AsQueryable()
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
                if (user == null)
                    throw new UnauthorizedAccessException("User not found.");

                bool alreadyOwnedInTx = await userGameRepo.AsQueryable()
                    .AnyAsync(ug => ug.UserId == userId && ug.GameId == gameId);
                if (alreadyOwnedInTx)
                    throw new InvalidOperationException("You already own this game.");

                if (user.Balance < actualPrice)
                    throw new InvalidOperationException(
                        $"Недостатньо коштів. Ціна гри: {actualPrice:N2} ₴, баланс: {user.Balance:N2} ₴.");

                user.Balance -= actualPrice;
                userRepo.Update(user);

                var now = DateTime.UtcNow;

                var coverImage = !string.IsNullOrWhiteSpace(gameDetails.Thumbnail)
                    ? gameDetails.Thumbnail
                    : $"https://cdn.akamai.steamstatic.com/steam/apps/{gameId}/header.jpg";

                var userGame = new UserGame
                {
                    UserId = userId,
                    GameId = gameId,
                    Title = gameDetails.Title,
                    ImageUrl = coverImage,
                    HeaderImage = coverImage,
                    Developer = gameDetails.Developer,
                    ReleaseDate = gameDetails.ReleaseDate,
                    Price = actualPrice,
                    AcquiredAt = now
                };
                await userGameRepo.AddAsync(userGame);

                if (actualPrice > 0)
                {
                    var walletTx = new WalletTransaction
                    {
                        UserId = userId,
                        Amount = -actualPrice,
                        Type = TransactionType.Purchase,
                        Title = $"Purchase: {gameDetails.Title} (AppID: {gameId})",
                        CreatedAt = now
                    };
                    await _uow.Repository<WalletTransaction>().AddAsync(walletTx);
                }

                var wishlistRepo = _uow.Repository<WishlistItem>();
                var wishItem = await wishlistRepo.AsQueryable()
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.GameId == gameId);
                if (wishItem != null)
                    wishlistRepo.Remove(wishItem);

                var cartRepo = _uow.Repository<CartItem>();
                var cartItem = await cartRepo.AsQueryable()
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.GameId == gameId);
                if (cartItem != null)
                    cartRepo.Remove(cartItem);

                await _uow.CommitTransactionAsync();

                return new PurchaseResultDto
                {
                    Success = true,
                    Message = "Game purchased successfully.",
                    GameId = gameId,
                    Title = gameDetails.Title,
                    PaidAmount = actualPrice,
                    Currency = "UAH",
                    NewBalance = user.Balance,
                    PurchasedAt = now.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
            }
            catch (DbUpdateException)
            {
                await _uow.RollbackTransactionAsync();
                throw new InvalidOperationException("Purchase conflict detected: you already own this game or a parallel transaction is in progress.");
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task<CheckoutResultDto> CheckoutCartAsync(Guid userId)
    {
        var userLock = _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync();

        try
        {
            var userRepo = _uow.Repository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
                throw new UnauthorizedAccessException("User not found.");

            var cartRepo = _uow.Repository<CartItem>();
            var cartItems = await cartRepo.AsQueryable()
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                throw new InvalidOperationException("Your cart is empty.");

            var userGameRepo = _uow.Repository<UserGame>();
            var ownedGameIds = await userGameRepo.AsQueryable()
                .Where(ug => ug.UserId == userId)
                .Select(ug => ug.GameId)
                .ToListAsync();

            var validatedGames = new List<(CartItem CartItem, string Title, string Cover, string? Dev, string? Release, decimal ActualPrice)>();

            foreach (var item in cartItems)
            {
                if (ownedGameIds.Contains(item.GameId))
                    throw new InvalidOperationException($"Game '{item.Title}' ({item.GameId}) is already in your library.");

                var details = await _steamService.GetGameDetailsAsync(item.GameId);
                if (details == null)
                    throw new KeyNotFoundException($"Game '{item.Title}' ({item.GameId}) was not found in Steam catalog.");

                decimal priceUsd = ExtractValidatedPrice(details.Price, details.IsFree);
                decimal actualPrice = ConvertUsdToUah(priceUsd);

                var cover = !string.IsNullOrWhiteSpace(details.Thumbnail)
                    ? details.Thumbnail
                    : item.ImageUrl;

                validatedGames.Add((item, details.Title, cover, details.Developer, details.ReleaseDate, actualPrice));
            }

            decimal totalCost = validatedGames.Sum(x => x.ActualPrice);

            if (user.Balance < totalCost)
                throw new InvalidOperationException(
                    $"Недостатньо коштів. Загальна сума: {totalCost:N2} ₴, баланс: {user.Balance:N2} ₴.");

            await _uow.BeginTransactionAsync();
            try
            {
                user = await userRepo.AsQueryable()
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
                if (user == null)
                    throw new UnauthorizedAccessException("User not found.");

                var requestedIds = validatedGames.Select(g => g.CartItem.GameId).ToList();
                bool anyAlreadyOwnedInTx = await userGameRepo.AsQueryable()
                    .AnyAsync(ug => ug.UserId == userId && requestedIds.Contains(ug.GameId));
                if (anyAlreadyOwnedInTx)
                    throw new InvalidOperationException("One or more games in your cart are already in your library.");

                if (user.Balance < totalCost)
                    throw new InvalidOperationException(
                        $"Недостатньо коштів. Загальна сума: {totalCost:N2} ₴, баланс: {user.Balance:N2} ₴.");

                user.Balance -= totalCost;
                userRepo.Update(user);

                var wishlistRepo = _uow.Repository<WishlistItem>();
                var txRepo = _uow.Repository<WalletTransaction>();
                var now = DateTime.UtcNow;
                var results = new List<PurchaseResultDto>();

                foreach (var game in validatedGames)
                {
                    await userGameRepo.AddAsync(new UserGame
                    {
                        UserId = userId,
                        GameId = game.CartItem.GameId,
                        Title = game.Title,
                        ImageUrl = game.Cover,
                        HeaderImage = game.Cover,
                        Developer = game.Dev,
                        ReleaseDate = game.Release,
                        Price = game.ActualPrice,
                        AcquiredAt = now
                    });

                    if (game.ActualPrice > 0)
                    {
                        await txRepo.AddAsync(new WalletTransaction
                        {
                            UserId = userId,
                            Amount = -game.ActualPrice,
                            Type = TransactionType.Purchase,
                            Title = $"Purchase: {game.Title} (AppID: {game.CartItem.GameId})",
                            CreatedAt = now
                        });
                    }

                    var wishItem = await wishlistRepo.AsQueryable()
                        .FirstOrDefaultAsync(w => w.UserId == userId && w.GameId == game.CartItem.GameId);
                    if (wishItem != null)
                        wishlistRepo.Remove(wishItem);

                    cartRepo.Remove(game.CartItem);

                    results.Add(new PurchaseResultDto
                    {
                        Success = true,
                        Message = "Purchased",
                        GameId = game.CartItem.GameId,
                        Title = game.Title,
                        PaidAmount = game.ActualPrice,
                        Currency = "UAH",
                        NewBalance = user.Balance,
                        PurchasedAt = now.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    });
                }

                await _uow.CommitTransactionAsync();

                return new CheckoutResultDto
                {
                    Success = true,
                    Message = $"Successfully purchased {results.Count} game(s).",
                    PurchasedCount = results.Count,
                    TotalPaid = totalCost,
                    Currency = "UAH",
                    NewBalance = user.Balance,
                    Items = results
                };
            }
            catch (DbUpdateException)
            {
                await _uow.RollbackTransactionAsync();
                throw new InvalidOperationException("Checkout conflict detected: one of the games is already owned or a parallel purchase is in progress.");
            }
            catch
            {
                await _uow.RollbackTransactionAsync();
                throw;
            }
        }
        finally
        {
            userLock.Release();
        }
    }

    private static decimal ExtractValidatedPrice(decimal price, bool isFree)
    {
        if (price < 0)
            throw new InvalidOperationException("Game price is invalid.");

        if (price == 0 && !isFree)
            throw new InvalidOperationException("Game price is unavailable for purchase. It might be unreleased or delisted.");

        return Math.Round(price, 2);
    }

    private static decimal ConvertUsdToUah(decimal priceUsd)
    {
        if (priceUsd <= 0)
            return 0;

        return Math.Round(priceUsd * UsdToUahRate, 2);
    }
}