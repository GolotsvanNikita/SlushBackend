using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Audit;
using Slush.Application.DTOs.Common;
using Slush.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Slush.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class AuditController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuditController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("logs")]
        public async Task<ActionResult<PagedResultDto<AdminActionLogDto>>> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.AdminActionLogs.AsQueryable();

            int totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new AdminActionLogDto(
                    l.Id,
                    l.AdminUsername,
                    l.Action,
                    l.EntityName,
                    l.EntityId,
                    l.Details,
                    l.Timestamp))
                .ToListAsync();

            return Ok(new PagedResultDto<AdminActionLogDto>(items, totalCount, page, pageSize));
        }
    }
}