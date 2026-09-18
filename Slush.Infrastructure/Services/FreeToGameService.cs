using Slush.Application.DTOs.Catalog;
using Slush.Application.Interfaces;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Slush.Infrastructure.Services;

public class FreeToGameService : IFreeToGameService
{
    private readonly HttpClient _httpClient;

    public FreeToGameService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<FreeToGameDto>> GetAllGamesAsync()
    {
        var url = "https://www.freetogame.com/api/games";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var jsonString = await response.Content.ReadAsStringAsync();

        var games = JsonSerializer.Deserialize<IEnumerable<FreeToGameDto>>(jsonString);

        return games ?? new List<FreeToGameDto>();
    }
}