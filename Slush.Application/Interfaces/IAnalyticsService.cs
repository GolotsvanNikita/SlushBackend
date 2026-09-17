using Slush.Application.DTOs.Analytics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.Interfaces
{
    public interface IAnalyticsService
    {
        Task<IEnumerable<RetentionPointDto>> GetRetentionAsync();
        Task<IEnumerable<ChurnRiskDto>> GetChurnRiskAsync(int inactivityThresholdDays = 14);
        Task<IEnumerable<CohortRowDto>> GetCohortsAsync();
        Task<PeriodComparisonDto> GetComparisonAsync(DateTime fromA, DateTime toA, DateTime fromB, DateTime toB);
    }
}
