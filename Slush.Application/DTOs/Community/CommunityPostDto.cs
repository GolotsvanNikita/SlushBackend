namespace Slush.Application.DTOs.Community;

public class CommunityPostDto
{
    public string Id { get; set; } = string.Empty;

    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorAvatarUrl { get; set; } = string.Empty;

    public string PostType { get; set; } = string.Empty;
    public bool IsLiked { get; set; }

    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ShortDescription { get; set; }
    public string? MediaUrl { get; set; }

    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }

    public string CreatedAt { get; set; } = string.Empty;
}