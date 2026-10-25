using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Slush.API.Hubs;
using Slush.Application.DTOs.Chat;
using Slush.Application.Interfaces;
using Slush.Domain.Enums;
using System.Security.Claims;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IHubContext<ChatHub> hubContext,
        INotificationService notificationService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] string? search = null)
    {
        var result = await _chatService.GetConversationsAsync(GetUserId(), search);
        return Ok(result);
    }

    [HttpGet("{friendId}/messages")]
    public async Task<IActionResult> GetMessages(Guid friendId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _chatService.GetMessagesAsync(GetUserId(), friendId, page, pageSize);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost("{friendId}/messages")]
    public async Task<IActionResult> SendMessage(Guid friendId, [FromBody] SendChatMessageRequestDto request)
    {
        var senderId = GetUserId();

        try
        {
            var messageDto = await _chatService.SendMessageAsync(senderId, friendId, request);

            bool receiverNotificationsEnabled = await _chatService.GetNotificationSettingAsync(friendId, senderId);

            if (receiverNotificationsEnabled)
            {
                var preview = messageDto.MessageType switch
                {
                    "Image" => "📷 Фото",
                    "File" => $"📎 {messageDto.FileName ?? "Файл"}",
                    "Voice" => "🎤 Голосове повідомлення",
                    _ => messageDto.Content ?? "Нове повідомлення"
                };

                try
                {
                    await _notificationService.SendNotificationAsync(
                        friendId,
                        $"{messageDto.SenderUsername}: {preview}",
                        "ChatMessage",
                        senderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Не вдалося створити сповіщення ChatMessage для користувача {UserId}", friendId);
                }
            }

            var messageForReceiver = new ChatMessageDto
            {
                Id = messageDto.Id,
                SenderId = messageDto.SenderId,
                SenderUsername = messageDto.SenderUsername,
                SenderAvatarUrl = messageDto.SenderAvatarUrl,
                ReceiverId = messageDto.ReceiverId,
                IsMine = false,
                MessageType = messageDto.MessageType,
                Content = messageDto.Content,
                MediaUrl = messageDto.MediaUrl,
                FileName = messageDto.FileName,
                FileSize = messageDto.FileSize,
                FormattedFileSize = messageDto.FormattedFileSize,
                DurationSeconds = messageDto.DurationSeconds,
                FormattedDuration = messageDto.FormattedDuration,
                IsRead = false,
                CreatedAt = messageDto.CreatedAt,
                TimeFormatted = messageDto.TimeFormatted,
                DateFormatted = messageDto.DateFormatted
            };

            await _hubContext.Clients
                .Group($"user_{friendId}")
                .SendAsync("ReceiveChatMessage", new
                {
                    message = messageForReceiver,
                    shouldNotify = receiverNotificationsEnabled
                });

            return Ok(messageDto);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPut("{friendId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid friendId)
    {
        var userId = GetUserId();

        var readCount = await _chatService.MarkConversationAsReadAsync(userId, friendId);

        await _notificationService.MarkRelatedNotificationsAsReadAsync(userId, "ChatMessage", friendId);

        return Ok(new { readCount });
    }

    [HttpGet("{friendId}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid friendId, [FromQuery] ChatMessageType type = ChatMessageType.Image)
    {
        try
        {
            var result = await _chatService.GetSharedAttachmentsAsync(GetUserId(), friendId, type);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPut("{friendId}/notifications")]
    public async Task<IActionResult> ToggleNotifications(Guid friendId, [FromBody] ToggleChatNotificationsRequestDto request)
    {
        try
        {
            var enabled = await _chatService.SetNotificationSettingAsync(GetUserId(), friendId, request.NotificationsEnabled);
            return Ok(new { friendId, notificationsEnabled = enabled });
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPut("{friendId}/block")]
    public async Task<IActionResult> ToggleBlock(Guid friendId, [FromBody] ToggleChatBlockRequestDto request)
    {
        try
        {
            var isBlocked = await _chatService.SetBlockStatusAsync(GetUserId(), friendId, request.IsBlocked);
            return Ok(new
            {
                friendId,
                isBlocked,
                message = isBlocked ? "Користувача заблоковано." : "Користувача розблоковано."
            });
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpDelete("{friendId}/history")]
    public async Task<IActionResult> ClearHistory(Guid friendId)
    {
        try
        {
            await _chatService.ClearHistoryAsync(GetUserId(), friendId);
            return Ok(new { message = "Історію чату очищено." });
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPost("{friendId}/report")]
    public async Task<IActionResult> ReportUser(Guid friendId, [FromBody] ReportChatUserRequestDto request)
    {
        try
        {
            await _chatService.ReportUserAsync(GetUserId(), friendId, request);
            return Ok(new { message = "Скаргу успішно надіслано модераторам." });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        try
        {
            await _chatService.DeleteMessageAsync(GetUserId(), messageId);
            return Ok(new { message = "Повідомлення успішно видалено." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
    }
}