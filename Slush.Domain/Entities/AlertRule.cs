using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Domain.Entities
{
    public class AlertRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RuleName { get; set; } = string.Empty;
        public RuleType RuleType { get; set; }
        public ConditionType Condition { get; set; }
        public int Threshold { get; set; }
        public NotificationChannel Channel { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
