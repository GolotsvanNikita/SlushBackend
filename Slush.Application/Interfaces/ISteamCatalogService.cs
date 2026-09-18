using Slush.Application.DTOs.Catalog;

namespace Slush.Application.Interfaces;

public interface ISteamCatalogService
{
    Task<UnifiedGameDetailsDto?> GetGameDetailsAsync(string appId);
}