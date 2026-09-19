using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
using Slush.Domain.Enums;

namespace Slush.Application.Interfaces
{
    public interface ICatalogService
    {
        Task<PagedResultDto<UnifiedGameDto>> SearchAsync(string query, GameSource? source, int? minDiscount, int page, int pageSize);
    }
}
