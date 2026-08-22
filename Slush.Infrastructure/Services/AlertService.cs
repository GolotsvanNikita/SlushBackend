using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Alerts;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slush.Infrastructure.Services
{
    public class AlertService : IAlertService
    {
        private readonly AppDbContext _context;

        public AlertService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AlertRuleDto>> GetRulesAsync()
        {
            var rules = await _context.AlertRules.ToListAsync();
            return rules.Select(r => new AlertRuleDto(r.Id, r.RuleName, r.RuleType, r.Condition, r.Threshold, r.Channel, r.IsEnabled));
        }

        public async Task<AlertRuleDto> CreateRuleAsync(CreateAlertRuleRequestDto dto)
        {
            var rule = new AlertRule
            {
                RuleName = dto.RuleName,
                RuleType = dto.RuleType,
                Condition = dto.Condition,
                Threshold = dto.Threshold,
                Channel = dto.Channel,
                IsEnabled = dto.IsEnabled
            };

            _context.AlertRules.Add(rule);
            await _context.SaveChangesAsync();

            return new AlertRuleDto(rule.Id, rule.RuleName, rule.RuleType, rule.Condition, rule.Threshold, rule.Channel, rule.IsEnabled);
        }

        public async Task<PagedResultDto<AlertHistoryDto>> GetHistoryAsync(int page, int pageSize)
        {
            var totalCount = await _context.AlertHistories.CountAsync();
            var items = await _context.AlertHistories
                .OrderByDescending(h => h.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new AlertHistoryDto(h.Id, h.RuleName, h.Timestamp, h.Message, h.IsRead))
                .ToListAsync();

            return new PagedResultDto<AlertHistoryDto>(items, totalCount, page, pageSize);
        }
    }
}