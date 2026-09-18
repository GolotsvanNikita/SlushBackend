using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Catalog;
using Slush.Application.Interfaces;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FreeGamesController : ControllerBase
{
    private readonly IFreeToGameService _freeToGameService;

    public FreeGamesController(IFreeToGameService freeToGameService)
    {
        _freeToGameService = freeToGameService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FreeToGameDto>>> GetGames()
    {
        var games = await _freeToGameService.GetAllGamesAsync();
        return Ok(games);
    }
}