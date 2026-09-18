using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Alerts;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slush.Infrastructure.Services
{
    public class AlertService : IAlertService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public AlertService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AlertRuleDto>> GetRulesAsync()
        {
            var rules = await _uow.Repository<AlertRule>().GetAllAsync();
            return _mapper.Map<IEnumerable<AlertRuleDto>>(rules);
        }

        public async Task<AlertRuleDto> CreateRuleAsync(CreateAlertRuleRequestDto dto)
        {
            var rule = _mapper.Map<AlertRule>(dto);

            await _uow.Repository<AlertRule>().AddAsync(rule);
            await _uow.SaveChangesAsync();

            return _mapper.Map<AlertRuleDto>(rule);
        }

        public async Task<PagedResultDto<AlertHistoryDto>> GetHistoryAsync(int page, int pageSize)
        {
            var query = _uow.Repository<AlertHistory>().AsQueryable();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(h => h.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<AlertHistoryDto>>(items);
            return new PagedResultDto<AlertHistoryDto>(dtos, totalCount, page, pageSize);
        }
    }
}