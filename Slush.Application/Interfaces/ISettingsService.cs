using Slush.Application.DTOs.Profile;

namespace Slush.Application.Interfaces;

public interface ISettingsService
{
    Task<ProfileSettingsDto> GetProfileSettingsAsync(Guid userId);
    Task UpdateProfileSettingsAsync(Guid userId, UpdateProfileSettingsDto request);

    Task<NotificationSettingsDto> GetNotificationSettingsAsync(Guid userId);
    Task UpdateNotificationSettingsAsync(Guid userId, NotificationSettingsDto request);
}