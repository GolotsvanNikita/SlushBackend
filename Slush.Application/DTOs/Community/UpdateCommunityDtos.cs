namespace Slush.Application.DTOs.Community;

public class UpdateCommunityPostDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ShortDescription { get; set; }
}

public class UpdateCommentDto
{
    public string Content { get; set; } = string.Empty;
}