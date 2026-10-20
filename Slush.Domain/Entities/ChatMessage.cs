using Slush.Domain.Enums;

namespace Slush.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public Guid ReceiverId { get; set; }
    public User Receiver { get; set; } = null!;

    public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

    public string? Content { get; set; }

    public string? MediaUrl { get; set; }

    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public int? DurationSeconds { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}