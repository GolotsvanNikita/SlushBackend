using System;

namespace Slush.Domain.Entities
{
    public class UserLoginHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }

        public DateTime LoginTimestamp { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}