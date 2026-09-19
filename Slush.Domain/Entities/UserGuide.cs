using System;

namespace Slush.Domain.Entities;

public class UserGuide
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public int LikesCount { get; set; }
    public DateTime CreatedAt { get; set; }
}