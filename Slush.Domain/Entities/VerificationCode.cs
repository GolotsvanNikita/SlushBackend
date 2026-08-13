using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Domain.Entities
{
    public class VerificationCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public CodePurpose Purpose { get; set; }
    }
}
