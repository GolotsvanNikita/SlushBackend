using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Alerts
{
    public record AlertRuleDto(
        Guid Id,
        string RuleName,
        RuleType RuleType,
        ConditionType Condition,
        int Threshold,
        NotificationChannel Channel,
        bool IsEnabled
    );
}
