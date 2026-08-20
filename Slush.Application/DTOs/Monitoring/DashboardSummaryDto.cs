using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Monitoring
{
    public record DashboardSummaryDto(
        int TotalMembers,
        int OnlineMembers,
        int ActiveSessionsCount,
        double NetworkStabilityPercent,
        int ActiveAlertsCount
    );
}
