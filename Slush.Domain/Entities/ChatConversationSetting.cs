namespace Slush.Domain.Entities;

public class ChatConversationSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid FriendId { get; set; }
    public User Friend { get; set; } = null!;

    public bool NotificationsEnabled { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}