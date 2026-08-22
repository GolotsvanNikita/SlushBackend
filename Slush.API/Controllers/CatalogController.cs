using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Catalog;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Enums;
using System.Threading.Tasks;

namespace Slush.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogService _catalogService;

        public CatalogController(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<PagedResultDto<UnifiedGameDto>>> Search(
            [FromQuery] string query = "",
            [FromQuery] GameSource? source = null,
            [FromQuery] int? minDiscount = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _catalogService.SearchAsync(query, source, minDiscount, page, pageSize);
            return Ok(result);
        }
    }
}