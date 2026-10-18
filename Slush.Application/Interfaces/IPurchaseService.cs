using Slush.Application.DTOs.Purchases;

namespace Slush.Application.Interfaces;

public interface IPurchaseService
{
    Task<PurchaseResultDto> PurchaseGameAsync(Guid userId, string gameId);
    Task<CheckoutResultDto> CheckoutCartAsync(Guid userId);
}