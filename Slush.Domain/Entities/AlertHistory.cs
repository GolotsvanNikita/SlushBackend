using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Domain.Entities
{
    public class AlertHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RuleName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}
