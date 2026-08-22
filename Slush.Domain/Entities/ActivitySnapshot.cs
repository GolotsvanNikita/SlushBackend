using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Domain.Entities
{
    public class ActivitySnapshot
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int ActiveCount { get; set; }
    }
}
