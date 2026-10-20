using Slush.Domain.Enums;

namespace Slush.Application.DTOs.Chat;

public class ChatConversationDto
{
    public Guid FriendId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }

    public string LastMessagePreview { get; set; } = "Немає повідомлень";
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessageDateFormatted { get; set; }

    public int UnreadCount { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string SenderAvatarUrl { get; set; } = string.Empty;
    public Guid ReceiverId { get; set; }
    public bool IsMine { get; set; }

    public string MessageType { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? MediaUrl { get; set; }

    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? FormattedFileSize { get; set; }

    public int? DurationSeconds { get; set; }
    public string? FormattedDuration { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeFormatted { get; set; } = string.Empty;
    public string DateFormatted { get; set; } = string.Empty;
}

public class SendChatMessageRequestDto
{
    public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;
    public string? Content { get; set; }
    public string? MediaUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public int? DurationSeconds { get; set; }
}

public class ChatAttachmentMonthGroupDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthTitle { get; set; } = string.Empty;
    public List<ChatAttachmentItemDto> Items { get; set; } = [];
}

public class ChatAttachmentItemDto
{
    public Guid MessageId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;

    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? FormattedFileSize { get; set; }

    public int? DurationSeconds { get; set; }
    public string? FormattedDuration { get; set; }

    public DateTime CreatedAt { get; set; }
    public string FormattedDateTime { get; set; } = string.Empty;
}

public class ToggleChatNotificationsRequestDto
{
    public bool NotificationsEnabled { get; set; }
}