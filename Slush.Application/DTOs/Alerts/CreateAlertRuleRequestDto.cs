using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Slush.Application.DTOs.Alerts
{
    public record CreateAlertRuleRequestDto(
        [Required(ErrorMessage = "Rule name is required.")] string RuleName,
        [Required] RuleType RuleType,
        [Required] ConditionType Condition,
        [Required] int Threshold,
        [Required] NotificationChannel Channel,
        bool IsEnabled = true
    );
}
