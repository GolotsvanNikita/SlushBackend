using Slush.Application.DTOs.Alerts;
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
    }
}
