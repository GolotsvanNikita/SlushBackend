using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Domain.Entities
{
    public class WalletTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public decimal Amount { get; set; }
        public string Title { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
