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

        if (data.TryGetProperty("metacritic", out var metacritic) && metacritic.TryGetProperty("score", out var score))
        {
            result.AverageRating = Math.Round(score.GetDouble() / 20, 1);
        }

        if (data.TryGetProperty("dlc", out var dlcs))
        {
            var dlcIds = dlcs.EnumerateArray().Select(d => d.GetInt32()).Take(10).ToList();

            if (dlcIds.Any())
            {
                var dlcUrl = $"https://store.steampowered.com/api/appdetails?appids={string.Join(",", dlcIds)}&cc=us&l=english";
                var dlcResponse = await _httpClient.GetAsync(dlcUrl);

                if (dlcResponse.IsSuccessStatusCode)
                {
                    var dlcJson = await dlcResponse.Content.ReadAsStringAsync();
                    using var dlcDoc = JsonDocument.Parse(dlcJson);

                    foreach (var dlcId in dlcIds)
                    {
                        var idStr = dlcId.ToString();
                        if (dlcDoc.RootElement.TryGetProperty(idStr, out var dItem) && dItem.GetProperty("success").GetBoolean())
                        {
                            var dData = dItem.GetProperty("data");
                            result.DLCs.Add(new DlcDto
                            {
                                Id = idStr,
                                Title = dData.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                                Description = dData.TryGetProperty("short_description", out var sd) ? sd.GetString() ?? "" : "",
                                Image = dData.TryGetProperty("header_image", out var hi) ? hi.GetString() ?? "" : "",
                                Price = dData.TryGetProperty("price_overview", out var po) ? po.GetProperty("final").GetInt32() / 100m : 0
                            });
                        }
                    }
                }
            }
        }

        if (data.TryGetProperty("packages", out var packages))
        {
            var packageIds = packages.EnumerateArray().Select(p => p.GetInt32()).Take(5).ToList();

            if (packageIds.Any())
            {
                var pkgUrl = $"https://store.steampowered.com/api/packagedetails?packageids={string.Join(",", packageIds)}&cc=us&l=english";
                var pkgResponse = await _httpClient.GetAsync(pkgUrl);

                if (pkgResponse.IsSuccessStatusCode)
                {
                    var pkgJson = await pkgResponse.Content.ReadAsStringAsync();
                    using var pkgDoc = JsonDocument.Parse(pkgJson);

                    foreach (var pkgId in packageIds)
                    {
                        var idStr = pkgId.ToString();
                        if (pkgDoc.RootElement.TryGetProperty(idStr, out var pItem) && pItem.GetProperty("success").GetBoolean())
                        {
                            var pData = pItem.GetProperty("data");

                            var bundleDesc = pData.TryGetProperty("page_content", out var pc) ? pc.GetString() ?? "" : "";
                            if (bundleDesc.Length > 200) bundleDesc = bundleDesc.Substring(0, 200) + "...";

                            result.Bundles.Add(new BundleDto
                            {
                                Id = idStr,
                                Title = pData.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                                Description = bundleDesc,
                                Image = pData.TryGetProperty("header_image", out var hi) ? hi.GetString() ?? (pData.TryGetProperty("small_logo", out var sl) ? sl.GetString() ?? "" : "") : "",
                                Price = pData.TryGetProperty("price", out var priceInfo) ? priceInfo.GetProperty("final").GetInt32() / 100m : 0
                            });
                        }
                    }
                }
            }
        }

        return result;
    }
}
