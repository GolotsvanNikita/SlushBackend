using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Profile
{
    public class WalletTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}
