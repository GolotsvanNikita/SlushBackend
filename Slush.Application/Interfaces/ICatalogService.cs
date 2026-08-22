using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.Interfaces
{
    public interface ICatalogService
    {
        Task<PagedResultDto<UnifiedGameDto>> SearchAsync(string query, GameSource? source, int? minDiscount, int page, int pageSize);
    }
}
