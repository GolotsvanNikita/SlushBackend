using System;

namespace Slush.Domain.Entities
{
    public class GameStatsSnapshot
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string GameName { get; set; } = string.Empty;
        public int ActivePlayers { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}