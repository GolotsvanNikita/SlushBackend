using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Alerts
{
    public record AlertHistoryDto(
        Guid Id,
        string RuleName,
        DateTime Timestamp,
        string Message,
        bool IsRead
    );
}