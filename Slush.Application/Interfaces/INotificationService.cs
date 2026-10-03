using Slush.Application.DTOs.Alerts;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;
using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.Interfaces
{
    public interface INotificationService
    {
        Task UpdateConfigAsync(NotificationChannel channel, UpdateNotificationChannelRequestDto request);
        Task<UpdateNotificationChannelRequestDto> GetConfigAsync(NotificationChannel channel);
        Task SendNotificationAsync(Guid userId, string message, string type);
        Task<PagedResultDto<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page, int pageSize);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid userId, Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
    }
}
