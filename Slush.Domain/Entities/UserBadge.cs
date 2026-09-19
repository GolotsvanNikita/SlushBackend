using System;

namespace Slush.Domain.Entities;

public class UserBadge
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BadgeId { get; set; }
    public Badge Badge { get; set; } = null!;

    public DateTime EarnedAt { get; set; }
}