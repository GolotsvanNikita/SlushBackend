using System;

namespace Slush.Domain.Entities;

public class CartItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string GameId { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}