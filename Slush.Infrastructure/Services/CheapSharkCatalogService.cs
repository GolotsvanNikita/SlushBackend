using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Enums;
using System.Globalization;
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

        public async Task<PagedResultDto<UnifiedGameDto>> SearchAsync(
            string query,
            GameSource? source,
            int? minDiscount,
            int page,
            int pageSize)
        {
            var rawDeals = new List<CheapSharkDeal>();
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 3; i++)
            {
                int cheapSharkPage = ((page - 1) * 3) + i;
                var url = $"https://www.cheapshark.com/api/1.0/deals?pageSize=60&pageNumber={cheapSharkPage}";

                if (!string.IsNullOrWhiteSpace(query))
                {
                    url += $"&title={Uri.EscapeDataString(query)}";
                }

                tasks.Add(_httpClient.GetAsync(url));
            }

            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var pageDeals = JsonSerializer.Deserialize<List<CheapSharkDeal>>(jsonString);
                    if (pageDeals != null)
                    {
                        rawDeals.AddRange(pageDeals);
                    }
                }
            }

            if (!rawDeals.Any())
            {
                return new PagedResultDto<UnifiedGameDto>(new List<UnifiedGameDto>(), 0, page, pageSize);
            }

            var itemsQuery = rawDeals
                .Where(d => !string.IsNullOrWhiteSpace(d.SteamAppID))
                .Select(d =>
                {
                    var price = ParseDecimal(d.SalePrice);
                    var oldPrice = ParseDecimal(d.NormalPrice);
                    var discount = ParseDecimal(d.Savings);

                    return new UnifiedGameDto(
                        d.SteamAppID!,
                        d.Title ?? "Unknown Title",
                        MapStoreID(d.StoreID),
                        price,
                        oldPrice,
                        (int)Math.Round(discount),
                        d.Thumb ?? "",
                        $"https://www.cheapshark.com/redirect?dealID={d.DealID}",
                        d.ReleaseDate?.ToString() ?? ""
                    );
                })
                .GroupBy(g => g.Id)
                .Select(group => group.OrderBy(g => g.Price).First())
                .AsQueryable();

            if (source.HasValue)
            {
                itemsQuery = itemsQuery.Where(g => g.Source == source.Value);
            }

            if (minDiscount.HasValue)
            {
                itemsQuery = itemsQuery.Where(g => g.DiscountPercent >= minDiscount.Value);
            }

            var finalItems = itemsQuery.Take(pageSize).ToList();

            var totalCount = finalItems.Count == pageSize ? page * pageSize + 10 : (page - 1) * pageSize + finalItems.Count;

            return new PagedResultDto<UnifiedGameDto>(
                finalItems,
                totalCount,
                page,
                pageSize
            );
        }

        private static decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return decimal.TryParse(
                value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var result
            )
                ? result
                : 0;
        }

        private static GameSource MapStoreID(string? storeId)
        {
            return storeId switch
            {
                "1" => GameSource.Steam,
                "7" => GameSource.GOG,
                "25" => GameSource.EpicGames,
                _ => GameSource.FreeToGame
            };
        }
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

        [JsonPropertyName("normalPrice")]
        public string? NormalPrice { get; set; }

        [JsonPropertyName("savings")]
        public string? Savings { get; set; }

        [JsonPropertyName("thumb")]
        public string? Thumb { get; set; }

        [JsonPropertyName("steamAppID")]
        public string? SteamAppID { get; set; }

        [JsonPropertyName("releaseDate")]
        public long? ReleaseDate { get; set; }
    }
}
