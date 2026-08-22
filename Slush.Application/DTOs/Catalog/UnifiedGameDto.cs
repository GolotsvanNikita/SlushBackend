using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Catalog
{
    public record UnifiedGameDto(
        Guid Id,
        string Title,
        GameSource Source,
        decimal Price,
        int DiscountPercent,
        string CoverUrl,
        string StoreUrl
    );
}
