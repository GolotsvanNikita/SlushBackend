using System;

namespace Slush.Domain.Entities;

public class ProfileComment
{
    public Guid Id { get; set; }

    public Guid ProfileUserId { get; set; }
    public User ProfileUser { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}