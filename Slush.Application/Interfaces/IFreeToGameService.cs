using Slush.Application.DTOs.Catalog;

namespace Slush.Application.Interfaces;

public interface IFreeToGameService
{
    Task<IEnumerable<FreeToGameDto>> GetAllGamesAsync();
}