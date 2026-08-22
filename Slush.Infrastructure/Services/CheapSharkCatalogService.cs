using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Enums;
using Slush.Infrastructure.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Slush.Infrastructure.Services
{
    public class CheapSharkCatalogService : ICatalogService
    {
        private readonly HttpClient _httpClient;

        public CheapSharkCatalogService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PagedResultDto<UnifiedGameDto>> SearchAsync(string query, GameSource? source, int? minDiscount, int page, int pageSize)
        {
            var safeQuery = string.IsNullOrWhiteSpace(query) ? "Batman" : Uri.EscapeDataString(query);
            var url = $"https://www.cheapshark.com/api/1.0/deals?title={safeQuery}&pageSize={pageSize}&pageNumber={page - 1}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return new PagedResultDto<UnifiedGameDto>(new List<UnifiedGameDto>(), 0, page, pageSize);
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var deals = JsonSerializer.Deserialize<List<CheapSharkDeal>>(jsonString);

            if (deals == null || !deals.Any())
                return new PagedResultDto<UnifiedGameDto>(new List<UnifiedGameDto>(), 0, page, pageSize);

            var items = deals.Select(d => new UnifiedGameDto(
                Guid.NewGuid(),
                d.Title,
                MapStoreID(d.StoreID),
                decimal.TryParse(d.SalePrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price) ? price : 0,
                decimal.TryParse(d.Savings, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var savings) ? (int)Math.Round(savings) : 0,
                d.Thumb,
                $"https://www.cheapshark.com/redirect?dealID={d.DealID}"
            )).ToList();

            var queryResult = items.AsQueryable();

            if (source.HasValue)
                queryResult = queryResult.Where(g => g.Source == source.Value);

            if (minDiscount.HasValue)
                queryResult = queryResult.Where(g => g.DiscountPercent >= minDiscount.Value);

            var finalItems = queryResult.ToList();
            return new PagedResultDto<UnifiedGameDto>(finalItems, 100, page, pageSize);
        }

        private GameSource MapStoreID(string storeId) => storeId switch
        {
            "1" => GameSource.Steam,
            "7" => GameSource.GOG,
            "25" => GameSource.EpicGames,
            _ => GameSource.FreeToGame
        };
    }
}