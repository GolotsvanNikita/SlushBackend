namespace Slush.Application.DTOs.Community;

public class CreateCommentDto
{
    public string Content { get; set; } = string.Empty;

    public Guid? ParentCommentId { get; set; }
}

public class PostCommentDto
{
    public Guid Id { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorAvatarUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;

    public List<PostCommentDto> Replies { get; set; } = new();
}