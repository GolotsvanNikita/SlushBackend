using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            var url = $"https://www.cheapshark.com/api/1.0/deals?pageSize={pageSize}&pageNumber={page - 1}";

            if (!string.IsNullOrWhiteSpace(query))
            {
                url += $"&title={Uri.EscapeDataString(query)}";
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return new PagedResultDto<UnifiedGameDto>(new List<UnifiedGameDto>(), 0, page, pageSize);
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var deals = JsonSerializer.Deserialize<List<CheapSharkDeal>>(jsonString);

            if (deals == null || !deals.Any())
                return new PagedResultDto<UnifiedGameDto>(new List<UnifiedGameDto>(), 0, page, pageSize);

            var items = deals
                .Where(d => !string.IsNullOrWhiteSpace(d.SteamAppID))
                .Select(d => new UnifiedGameDto(
                    d.SteamAppID!,
                    d.Title ?? "Unknown Title",
                    MapStoreID(d.StoreID),
                    decimal.TryParse(d.SalePrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price) ? price : 0,
                    decimal.TryParse(d.Savings, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var savings) ? (int)Math.Round(savings) : 0,
                    d.Thumb ?? "",
                    $"https://www.cheapshark.com/redirect?dealID={d.DealID}"
                )).AsQueryable();

            if (source.HasValue)
                items = items.Where(g => g.Source == source.Value);

            if (minDiscount.HasValue)
                items = items.Where(g => g.DiscountPercent >= minDiscount.Value);

            var finalItems = items.ToList();
            int totalCount = finalItems.Count == pageSize ? page * pageSize + 1 : (page - 1) * pageSize + finalItems.Count;

            return new PagedResultDto<UnifiedGameDto>(finalItems, totalCount, page, pageSize);
        }

        private GameSource MapStoreID(string? storeId) => storeId switch
        {
            "1" => GameSource.Steam,
            "7" => GameSource.GOG,
            "25" => GameSource.EpicGames,
            _ => GameSource.FreeToGame
        };
    }

    public class CheapSharkDeal
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("dealID")]
        public string? DealID { get; set; }

        [JsonPropertyName("storeID")]
        public string? StoreID { get; set; }

        [JsonPropertyName("salePrice")]
        public string? SalePrice { get; set; }

        [JsonPropertyName("savings")]
        public string? Savings { get; set; }

        [JsonPropertyName("thumb")]
        public string? Thumb { get; set; }

        [JsonPropertyName("steamAppID")]
        public string? SteamAppID { get; set; }
    }
}