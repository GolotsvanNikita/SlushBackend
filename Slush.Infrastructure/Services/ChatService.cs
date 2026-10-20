using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Chat;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;

namespace Slush.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;

    private static readonly string[] UkrainianMonths =
    [
        "", "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
        "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень"
    ];

    public ChatService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<ChatConversationDto>> GetConversationsAsync(Guid userId, string? search = null)
    {
        var friendships = await _uow.Repository<FriendRequest>().AsQueryable()
            .Include(r => r.Sender)
            .Include(r => r.Receiver)
            .Where(r => (r.SenderId == userId || r.ReceiverId == userId)
                        && r.Status == FriendRequestStatus.Accepted)
            .ToListAsync();

        var friends = friendships
            .Select(r => r.SenderId == userId ? r.Receiver : r.Sender)
            .Where(u => !u.IsDeleted)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            friends = friends.Where(f => f.Username.ToLower().Contains(term)).ToList();
        }

        if (!friends.Any()) return [];

        var friendIds = friends.Select(f => f.Id).ToList();

        var messages = await _uow.Repository<ChatMessage>().AsQueryable()
            .Where(m => (m.SenderId == userId && friendIds.Contains(m.ReceiverId)) ||
                        (m.ReceiverId == userId && friendIds.Contains(m.SenderId)))
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var settings = await _uow.Repository<ChatConversationSetting>().AsQueryable()
            .Where(s => s.UserId == userId && friendIds.Contains(s.FriendId))
            .ToDictionaryAsync(s => s.FriendId, s => s.NotificationsEnabled);

        var result = new List<ChatConversationDto>();

        foreach (var friend in friends)
        {
            var chatMessages = messages
                .Where(m => m.SenderId == friend.Id || m.ReceiverId == friend.Id)
                .ToList();

            var lastMsg = chatMessages.FirstOrDefault();
            var unreadCount = chatMessages.Count(m => m.SenderId == friend.Id && m.ReceiverId == userId && !m.IsRead);

            string preview = "Немає повідомлень";
            if (lastMsg != null)
            {
                string prefix = lastMsg.SenderId == userId ? "Ви: " : "";
                string body = lastMsg.MessageType switch
                {
                    ChatMessageType.Image => !string.IsNullOrWhiteSpace(lastMsg.Content) ? lastMsg.Content : "📷 Фото",
                    ChatMessageType.File => $"📎 {lastMsg.FileName ?? "Файл"}",
                    ChatMessageType.Voice => "🎤 Голосове повідомлення",
                    _ => lastMsg.Content ?? ""
                };
                preview = prefix + body;
            }

            result.Add(new ChatConversationDto
            {
                FriendId = friend.Id,
                Username = friend.Username,
                AvatarUrl = friend.AvatarUrl,
                LastSeenAt = friend.LastSeenAt,
                IsOnline = friend.LastSeenAt.HasValue &&
                           (DateTime.UtcNow - friend.LastSeenAt.Value) <= TimeSpan.FromMinutes(2),
                LastMessagePreview = preview,
                LastMessageAt = lastMsg?.CreatedAt,
                LastMessageDateFormatted = lastMsg != null
                    ? lastMsg.CreatedAt.ToString("dd MMM", new CultureInfo("uk-UA"))
                    : null,
                UnreadCount = unreadCount,
                NotificationsEnabled = !settings.TryGetValue(friend.Id, out var enabled) || enabled
            });
        }

        return result
            .OrderByDescending(c => c.LastMessageAt.HasValue)
            .ThenByDescending(c => c.LastMessageAt)
            .ThenBy(c => c.Username)
            .ToList();
    }

    public async Task<PagedResultDto<ChatMessageDto>> GetMessagesAsync(Guid userId, Guid friendId, int page = 1, int pageSize = 50)
    {
        await EnsureAreFriendsAsync(userId, friendId);

        await MarkConversationAsReadAsync(userId, friendId);

        var query = _uow.Repository<ChatMessage>().AsQueryable()
            .Include(m => m.Sender)
            .Where(m => (m.SenderId == userId && m.ReceiverId == friendId) ||
                        (m.SenderId == friendId && m.ReceiverId == userId))
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items
            .OrderBy(m => m.CreatedAt)
            .Select(m => MapToMessageDto(m, userId))
            .ToList();

        return new PagedResultDto<ChatMessageDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid senderId, Guid friendId, SendChatMessageRequestDto request)
    {
        await EnsureAreFriendsAsync(senderId, friendId);

        if (request.MessageType == ChatMessageType.Text && string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Message text cannot be empty.");

        if (request.MessageType != ChatMessageType.Text && string.IsNullOrWhiteSpace(request.MediaUrl))
            throw new ArgumentException($"MediaUrl is required for message type '{request.MessageType}'.");

        var msg = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = friendId,
            MessageType = request.MessageType,
            Content = request.Content?.Trim(),
            MediaUrl = request.MediaUrl?.Trim(),
            FileName = request.FileName,
            FileSize = request.FileSize,
            DurationSeconds = request.DurationSeconds,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Repository<ChatMessage>().AddAsync(msg);
        await _uow.SaveChangesAsync();

        var sender = await _uow.Repository<User>().GetByIdAsync(senderId);
        msg.Sender = sender!;

        return MapToMessageDto(msg, senderId);
    }

    public async Task<int> MarkConversationAsReadAsync(Guid userId, Guid friendId)
    {
        var repo = _uow.Repository<ChatMessage>();
        var unreadMessages = await repo.AsQueryable()
            .Where(m => m.SenderId == friendId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();

        if (!unreadMessages.Any()) return 0;

        var now = DateTime.UtcNow;
        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = now;
            repo.Update(msg);
        }

        await _uow.SaveChangesAsync();
        return unreadMessages.Count;
    }

    public async Task<List<ChatAttachmentMonthGroupDto>> GetSharedAttachmentsAsync(
        Guid userId, Guid friendId, ChatMessageType type)
    {
        await EnsureAreFriendsAsync(userId, friendId);

        if (type == ChatMessageType.Text)
            throw new ArgumentException("Attachment type must be Image, File, or Voice.");

        var attachments = await _uow.Repository<ChatMessage>().AsQueryable()
            .Include(m => m.Sender)
            .Where(m => ((m.SenderId == userId && m.ReceiverId == friendId) ||
                         (m.SenderId == friendId && m.ReceiverId == userId))
                        && m.MessageType == type
                        && m.MediaUrl != null)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var grouped = attachments
            .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Select(g => new ChatAttachmentMonthGroupDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthTitle = UkrainianMonths[g.Key.Month],
                Items = g.Select(m => new ChatAttachmentItemDto
                {
                    MessageId = m.Id,
                    SenderId = m.SenderId,
                    SenderUsername = m.Sender.Username,
                    MessageType = m.MessageType.ToString(),
                    MediaUrl = m.MediaUrl!,
                    FileName = m.FileName,
                    FileSize = m.FileSize,
                    FormattedFileSize = FormatFileSize(m.FileSize),
                    DurationSeconds = m.DurationSeconds,
                    FormattedDuration = FormatDuration(m.DurationSeconds),
                    CreatedAt = m.CreatedAt,
                    FormattedDateTime = m.CreatedAt.ToString("dd.MM.yyyy 'о' HH:mm")
                }).ToList()
            })
            .ToList();

        return grouped;
    }

    public async Task<bool> GetNotificationSettingAsync(Guid userId, Guid friendId)
    {
        var setting = await _uow.Repository<ChatConversationSetting>().AsQueryable()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.FriendId == friendId);

        return setting?.NotificationsEnabled ?? true;
    }

    public async Task<bool> SetNotificationSettingAsync(Guid userId, Guid friendId, bool enabled)
    {
        await EnsureAreFriendsAsync(userId, friendId);

        var repo = _uow.Repository<ChatConversationSetting>();
        var setting = await repo.AsQueryable()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.FriendId == friendId);

        if (setting == null)
        {
            setting = new ChatConversationSetting
            {
                UserId = userId,
                FriendId = friendId,
                NotificationsEnabled = enabled,
                UpdatedAt = DateTime.UtcNow
            };
            await repo.AddAsync(setting);
        }
        else
        {
            setting.NotificationsEnabled = enabled;
            setting.UpdatedAt = DateTime.UtcNow;
            repo.Update(setting);
        }

        await _uow.SaveChangesAsync();
        return setting.NotificationsEnabled;
    }

    private async Task EnsureAreFriendsAsync(Guid userId, Guid friendId)
    {
        bool areFriends = await _uow.Repository<FriendRequest>().AsQueryable()
            .AnyAsync(r => ((r.SenderId == userId && r.ReceiverId == friendId) ||
                            (r.SenderId == friendId && r.ReceiverId == userId))
                           && r.Status == FriendRequestStatus.Accepted);

        if (!areFriends)
            throw new UnauthorizedAccessException("You can only chat with users from your friends list.");
    }

    private static ChatMessageDto MapToMessageDto(ChatMessage m, Guid currentUserId) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderUsername = m.Sender?.Username ?? string.Empty,
        SenderAvatarUrl = m.Sender?.AvatarUrl ?? string.Empty,
        ReceiverId = m.ReceiverId,
        IsMine = m.SenderId == currentUserId,
        MessageType = m.MessageType.ToString(),
        Content = m.Content,
        MediaUrl = m.MediaUrl,
        FileName = m.FileName,
        FileSize = m.FileSize,
        FormattedFileSize = FormatFileSize(m.FileSize),
        DurationSeconds = m.DurationSeconds,
        FormattedDuration = FormatDuration(m.DurationSeconds),
        IsRead = m.IsRead,
        CreatedAt = m.CreatedAt,
        TimeFormatted = m.CreatedAt.ToString("HH:mm"),
        DateFormatted = m.CreatedAt.ToString("dd.MM.yyyy")
    };

    private static string? FormatFileSize(long? bytes)
    {
        if (!bytes.HasValue) return null;
        if (bytes.Value >= 1024 * 1024)
            return $"{Math.Round(bytes.Value / (1024d * 1024d), 1)} МБ";
        if (bytes.Value >= 1024)
            return $"{Math.Round(bytes.Value / 1024d, 1)} КБ";
        return $"{bytes.Value} Б";
    }

    private static string? FormatDuration(int? seconds)
    {
        if (!seconds.HasValue) return null;
        var ts = TimeSpan.FromSeconds(seconds.Value);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
    }
}