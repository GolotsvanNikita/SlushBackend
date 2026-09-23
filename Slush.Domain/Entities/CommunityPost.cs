using Slush.Domain.Enums;

namespace Slush.Domain.Entities;

public class CommunityPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string GameId { get; set; } = string.Empty;

    public CommunityPostType PostType { get; set; }

    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ShortDescription { get; set; }

    public string? MediaUrl { get; set; }

    public int LikesCount { get; set; } = 0;
    public int CommentsCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
}