using Slush.Application.DTOs.Catalog;
using Slush.Application.Interfaces;
using System.Text.Json;

namespace Slush.Infrastructure.Services;

public class SteamCatalogService : ISteamCatalogService
{
    private readonly HttpClient _httpClient;

    public SteamCatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UnifiedGameDetailsDto?> GetGameDetailsAsync(string appId)
    {
        var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=us&l=english";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode) return null;

        var jsonString = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(jsonString);
        var root = document.RootElement;

        if (!root.TryGetProperty(appId, out var appElement) || !appElement.GetProperty("success").GetBoolean())
        {
            return null;
        }

        var data = appElement.GetProperty("data");

        var result = new UnifiedGameDetailsDto
        {
            Id = appId,
            Title = data.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            Description = data.TryGetProperty("detailed_description", out var desc) ? desc.GetString() ?? "" : "",
            Thumbnail = data.TryGetProperty("header_image", out var img) ? img.GetString() ?? "" : "",
            Developer = data.TryGetProperty("developers", out var devs) && devs.GetArrayLength() > 0 ? devs[0].GetString() ?? "" : "",
            Publisher = data.TryGetProperty("publishers", out var pubs) && pubs.GetArrayLength() > 0 ? pubs[0].GetString() ?? "" : "",
        };

        if (data.TryGetProperty("release_date", out var releaseDate) && releaseDate.TryGetProperty("date", out var date))
        {
            result.ReleaseDate = date.GetString() ?? "";
        }

        if (data.TryGetProperty("price_overview", out var priceOverview))
        {
            result.OldPrice = priceOverview.GetProperty("initial").GetInt32() / 100m;
            result.Price = priceOverview.GetProperty("final").GetInt32() / 100m;
            result.DiscountPercent = priceOverview.GetProperty("discount_percent").GetInt32();
        }

        if (data.TryGetProperty("screenshots", out var screenshots))
        {
            foreach (var screen in screenshots.EnumerateArray())
            {
                result.Screenshots.Add(new ScreenshotDto
                {
                    Id = screen.GetProperty("id").GetInt32(),
                    Image = screen.GetProperty("path_full").GetString() ?? ""
                });
            }
        }

        if (data.TryGetProperty("genres", out var genres))
        {
            foreach (var genre in genres.EnumerateArray())
            {
                result.Tags.Add(genre.GetProperty("description").GetString() ?? "");
            }
        }

        if (data.TryGetProperty("dlc", out var dlcs))
        {
            foreach (var dlc in dlcs.EnumerateArray())
            {
                result.DLCs.Add(dlc.GetInt32());
            }
        }

        return result;
    }
}