using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Analytics
{
    public record PeriodComparisonDto(
        int UsersPeriodA,
        int UsersPeriodB,
        double UsersGrowthPercent,
        int ActivePeriodA,
        int ActivePeriodB,
        double ActiveGrowthPercent
    );
}
