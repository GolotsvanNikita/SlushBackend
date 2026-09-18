using System;

namespace Slush.Domain.Entities;

public class GameReview
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string GameId { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}