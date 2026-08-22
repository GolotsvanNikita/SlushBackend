using Slush.Application.DTOs.Alerts;
using Slush.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.Interfaces
{
    public interface IAlertService
    {
        Task<IEnumerable<AlertRuleDto>> GetRulesAsync();
        Task<AlertRuleDto> CreateRuleAsync(CreateAlertRuleRequestDto request);
        Task<PagedResultDto<AlertHistoryDto>> GetHistoryAsync(int page, int pageSize);
    }
}
