using System.ComponentModel.DataAnnotations;

namespace Slush.Application.DTOs.Profile;

public class ProfileReviewDto
{
    public string GameId { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public string GameBannerUrl { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class ProfileGuideDto
{
    public string GameTitle { get; set; } = string.Empty;
    public string GuideTitle { get; set; } = string.Empty;
    public string TextSnippet { get; set; } = string.Empty;
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class ProfileCommentDto
{
    public string Id { get; set; } = string.Empty;
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorAvatarUrl { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}