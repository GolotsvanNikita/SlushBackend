using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;

namespace Slush.Application.Interfaces;

public interface ISteamCatalogService
{
    Task<UnifiedGameDetailsDto?> GetGameDetailsAsync(string appId);
    Task<PagedResultDto<UnifiedGameDto>> SearchSteamGamesAsync(string query, int page, int pageSize);
}