using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Domain.Entities
{
    public class NotificationConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public NotificationChannel Channel { get; set; }
        public bool IsEnabled { get; set; } = false;

        public string? ConfigurationData { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
