using Slush.Application.DTOs.Chat;
using Slush.Application.DTOs.Common;
using Slush.Domain.Enums;

namespace Slush.Application.Interfaces;

public interface IChatService
{
    Task<List<ChatConversationDto>> GetConversationsAsync(Guid userId, string? search = null);
    Task<PagedResultDto<ChatMessageDto>> GetMessagesAsync(Guid userId, Guid friendId, int page = 1, int pageSize = 50);
    Task<ChatMessageDto> SendMessageAsync(Guid senderId, Guid friendId, SendChatMessageRequestDto request);
    Task<int> MarkConversationAsReadAsync(Guid userId, Guid friendId);
    Task<List<ChatAttachmentMonthGroupDto>> GetSharedAttachmentsAsync(Guid userId, Guid friendId, ChatMessageType type);
    Task<bool> GetNotificationSettingAsync(Guid userId, Guid friendId);
    Task<bool> SetNotificationSettingAsync(Guid userId, Guid friendId, bool enabled);
}