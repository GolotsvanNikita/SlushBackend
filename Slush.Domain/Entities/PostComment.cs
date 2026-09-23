namespace Slush.Domain.Entities;

public class PostComment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PostId { get; set; }
    public CommunityPost Post { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public Guid? ParentCommentId { get; set; }
    public PostComment? ParentComment { get; set; }
    public ICollection<PostComment> Replies { get; set; } = new List<PostComment>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}