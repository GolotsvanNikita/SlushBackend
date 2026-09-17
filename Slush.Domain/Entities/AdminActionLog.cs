using System;

namespace Slush.Domain.Entities
{
    public class AdminActionLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AdminId { get; set; }
        public string AdminUsername { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}