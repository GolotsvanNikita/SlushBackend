using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
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
            var pkgUrl = $"https://store.steampowered.com/api/packagedetails?packageids={appId}&cc=us&l=english";
            var pkgResponse = await _httpClient.GetAsync(pkgUrl);
            if (!pkgResponse.IsSuccessStatusCode) return null;

            var pkgJson = await pkgResponse.Content.ReadAsStringAsync();
            using var pkgDoc = JsonDocument.Parse(pkgJson);
            var pkgRoot = pkgDoc.RootElement;

            if (!pkgRoot.TryGetProperty(appId, out var pkgElement) || !pkgElement.GetProperty("success").GetBoolean())
                return null;

            var pData = pkgElement.GetProperty("data");

            var pkgResult = new UnifiedGameDetailsDto
            {
                Id = appId,
                Title = pData.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Description = pData.TryGetProperty("page_content", out var pc) ? pc.GetString() ?? "" : "",
                Thumbnail = pData.TryGetProperty("header_image", out var hi) ? hi.GetString() ?? "" : ""
            };

            if (pData.TryGetProperty("price", out var priceInfo) && priceInfo.ValueKind == JsonValueKind.Object)
            {
                pkgResult.Price = priceInfo.TryGetProperty("final", out var finalP) ? finalP.GetInt32() / 100m : 0;
                pkgResult.OldPrice = priceInfo.TryGetProperty("initial", out var initialP) ? initialP.GetInt32() / 100m : pkgResult.Price;
            }

            return pkgResult;
        }

        var data = appElement.GetProperty("data");

        var result = new UnifiedGameDetailsDto
        {
            Id = appId,
            Title = data.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
            Description = data.TryGetProperty("detailed_description", out var desc) ? desc.GetString() ?? "" : "",
            Thumbnail = data.TryGetProperty("header_image", out var img) ? img.GetString() ?? "" : "",
            Developer = data.TryGetProperty("developers", out var devs) && devs.ValueKind == JsonValueKind.Array && devs.GetArrayLength() > 0 ? devs[0].GetString() ?? "" : "",
            Publisher = data.TryGetProperty("publishers", out var pubs) && pubs.ValueKind == JsonValueKind.Array && pubs.GetArrayLength() > 0 ? pubs[0].GetString() ?? "" : "",
        };

        if (data.TryGetProperty("pc_requirements", out var pcReq) && pcReq.ValueKind == JsonValueKind.Object)
        {
            result.PcRequirements.Minimum = pcReq.TryGetProperty("minimum", out var min) ? min.GetString() ?? "" : "";
            result.PcRequirements.Recommended = pcReq.TryGetProperty("recommended", out var rec) ? rec.GetString() ?? "" : "";
        }

        if (data.TryGetProperty("release_date", out var releaseDate) && releaseDate.ValueKind == JsonValueKind.Object && releaseDate.TryGetProperty("date", out var date))
            result.ReleaseDate = date.GetString() ?? "";

        if (data.TryGetProperty("price_overview", out var priceOverview) && priceOverview.ValueKind == JsonValueKind.Object)
        {
            result.OldPrice = priceOverview.TryGetProperty("initial", out var initial) ? initial.GetInt32() / 100m : 0;
            result.Price = priceOverview.TryGetProperty("final", out var finalP) ? finalP.GetInt32() / 100m : 0;
            result.DiscountPercent = priceOverview.TryGetProperty("discount_percent", out var dp) ? dp.GetInt32() : 0;
        }

        if (data.TryGetProperty("screenshots", out var screenshots) && screenshots.ValueKind == JsonValueKind.Array)
        {
            foreach (var screen in screenshots.EnumerateArray())
            {
                result.Screenshots.Add(new ScreenshotDto
                {
                    Id = screen.TryGetProperty("id", out var sId) && sId.ValueKind == JsonValueKind.Number ? sId.GetInt32() : 0,
                    Image = screen.TryGetProperty("path_full", out var sImg) ? sImg.GetString() ?? "" : ""
                });
            }
        }

        if (data.TryGetProperty("genres", out var genres) && genres.ValueKind == JsonValueKind.Array)
        {
            foreach (var genre in genres.EnumerateArray())
                result.Tags.Add(genre.TryGetProperty("description", out var gDesc) ? gDesc.GetString() ?? "" : "");
        }

        if (data.TryGetProperty("metacritic", out var metacritic) && metacritic.ValueKind == JsonValueKind.Object && metacritic.TryGetProperty("score", out var score) && score.ValueKind == JsonValueKind.Number)
            result.AverageRating = Math.Round(score.GetDouble() / 20, 1);

        if (data.TryGetProperty("dlc", out var dlcs) && dlcs.ValueKind == JsonValueKind.Array)
        {
            var dlcIds = dlcs.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Number)
                .Select(item => item.GetInt32())
                .ToList();

            if (dlcIds.Any())
            {
                var semaphore = new SemaphoreSlim(3);
                var tasks = dlcIds.Select(async dlcId =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var dlcUrl = $"https://store.steampowered.com/api/appdetails?appids={dlcId}&cc=us&l=english";
                        var dlcResponse = await _httpClient.GetAsync(dlcUrl);

                        if (!dlcResponse.IsSuccessStatusCode) return null;

                        var dlcJson = await dlcResponse.Content.ReadAsStringAsync();
                        using var dlcDoc = JsonDocument.Parse(dlcJson);
                        var idStr = dlcId.ToString();

                        if (dlcDoc.RootElement.TryGetProperty(idStr, out var dItem) && dItem.ValueKind == JsonValueKind.Object && dItem.TryGetProperty("success", out var dSucc) && dSucc.GetBoolean())
                        {
                            if (dItem.TryGetProperty("data", out var dData) && dData.ValueKind == JsonValueKind.Object)
                            {
                                var title = dData.TryGetProperty("name", out var n) ? n.GetString() : "";


                                if (string.IsNullOrWhiteSpace(title) || title.Contains("Language", StringComparison.OrdinalIgnoreCase))
                                    return null;

                                decimal dlcPrice = 0;
                                if (dData.TryGetProperty("price_overview", out var po) && po.ValueKind == JsonValueKind.Object && po.TryGetProperty("final", out var finalPoPrice))
                                    dlcPrice = finalPoPrice.GetInt32() / 100m;

                                if (dlcPrice <= 0)
                                    return null;

                                return new DlcDto
                                {
                                    Id = idStr,
                                    Title = title,
                                    Description = dData.TryGetProperty("short_description", out var sd) ? sd.GetString() ?? "" : "",
                                    Image = dData.TryGetProperty("header_image", out var hi) ? hi.GetString() ?? "" : "",
                                    Price = dlcPrice
                                };
                            }
                        }
                        return null;
                    }
                    finally
                    {
                        await Task.Delay(150);
                        semaphore.Release();
                    }
                });

                var dlcResults = await Task.WhenAll(tasks);
                result.DLCs.AddRange(dlcResults.Where(d => d != null)!);
            }
        }


        if (data.TryGetProperty("packages", out var packages) && packages.ValueKind == JsonValueKind.Array)
        {
            var packageIds = packages.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Number).Select(p => p.GetInt32()).Take(5).ToList();

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
                        if (pkgDoc.RootElement.TryGetProperty(idStr, out var pItem) && pItem.ValueKind == JsonValueKind.Object && pItem.TryGetProperty("success", out var pSucc) && pSucc.GetBoolean())
                        {
                            if (pItem.TryGetProperty("data", out var pData) && pData.ValueKind == JsonValueKind.Object)
                            {
                                var bundleDesc = pData.TryGetProperty("page_content", out var pc) ? pc.GetString() ?? "" : "";
                                if (bundleDesc.Length > 200) bundleDesc = bundleDesc.Substring(0, 200) + "...";

                                decimal bundlePrice = 0;
                                if (pData.TryGetProperty("price", out var priceInfo) && priceInfo.ValueKind == JsonValueKind.Object && priceInfo.TryGetProperty("final", out var finalBPrice))
                                    bundlePrice = finalBPrice.GetInt32() / 100m;

                                result.Bundles.Add(new BundleDto
                                {
                                    Id = idStr,
                                    Title = pData.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                                    Description = bundleDesc,
                                    Image = pData.TryGetProperty("header_image", out var hi) ? hi.GetString() ?? (pData.TryGetProperty("small_logo", out var sl) ? sl.GetString() ?? "" : "") : "",
                                    Price = bundlePrice
                                });
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    public async Task<PagedResultDto<UnifiedGameDto>> SearchSteamGamesAsync(string query, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new PagedResultDto<UnifiedGameDto>(new List<UnifiedGameDto>(), 0, page, pageSize);

        var url = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(query)}&l=english&cc=us";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new PagedResultDto<UnifiedGameDto>(new List<UnifiedGameDto>(), 0, page, pageSize);

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        var root = doc.RootElement;

        var items = new List<UnifiedGameDto>();
        int total = 0;

        if (root.TryGetProperty("total", out var totalProp) && totalProp.ValueKind == JsonValueKind.Number)
            total = totalProp.GetInt32();

        if (root.TryGetProperty("items", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in itemsArray.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
                {
                    if (typeProp.GetString() != "app") continue;
                }

                var appId = item.GetProperty("id").GetInt32().ToString();
                var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

                decimal price = 0;
                decimal oldPrice = 0;

                if (item.TryGetProperty("price", out var priceProp))
                {
                    if (priceProp.ValueKind == JsonValueKind.Number)
                    {
                        price = priceProp.GetInt32() / 100m;
                        oldPrice = price;
                    }
                    else if (priceProp.ValueKind == JsonValueKind.Object)
                    {
                        price = priceProp.TryGetProperty("final", out var finalP) ? finalP.GetInt32() / 100m : 0;
                        oldPrice = priceProp.TryGetProperty("initial", out var initialP) ? initialP.GetInt32() / 100m : price;
                    }
                }

                items.Add(new UnifiedGameDto(
                    appId,
                    name,
                    Domain.Enums.GameSource.Steam,
                    price,
                    oldPrice,
                    0,
                    $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/header.jpg",
                    $"https://store.steampowered.com/app/{appId}",
                    ""
                ));
            }
        }

        var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResultDto<UnifiedGameDto>(pagedItems, total, page, pageSize);
    }
}