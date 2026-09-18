using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Audit;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public AuditController(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        [HttpGet("logs")]
        public async Task<ActionResult<PagedResultDto<AdminActionLogDto>>> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _uow.Repository<AdminActionLog>().AsQueryable();

            int totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var itemsDto = _mapper.Map<IEnumerable<AdminActionLogDto>>(items);

            return Ok(new PagedResultDto<AdminActionLogDto>(itemsDto, totalCount, page, pageSize));
        }
    }
}