using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Alerts;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace Slush.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IDataProtector _protector;

        public NotificationService(IUnitOfWork uow, IDataProtectionProvider dataProtectionProvider)
        {
            _uow = uow;
            _protector = dataProtectionProvider.CreateProtector("Slush.NotificationConfigs.SecureTokens");
        }

        public async Task UpdateConfigAsync(NotificationChannel channel, UpdateNotificationChannelRequestDto request)
        {
            var configRepo = _uow.Repository<NotificationConfig>();
            var config = await configRepo.AsQueryable().FirstOrDefaultAsync(c => c.Channel == channel);

            if (config == null)
            {
                config = new NotificationConfig { Channel = channel };
                await configRepo.AddAsync(config);
            }

            config.IsEnabled = request.IsEnabled;

            if (!string.IsNullOrEmpty(request.ConfigurationData))
            {
                config.ConfigurationData = _protector.Protect(request.ConfigurationData);
            }
            else
            {
                config.ConfigurationData = null;
            }

            config.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync();
        }

        public async Task<UpdateNotificationChannelRequestDto> GetConfigAsync(NotificationChannel channel)
        {
            var config = await _uow.Repository<NotificationConfig>().AsQueryable()
                .FirstOrDefaultAsync(c => c.Channel == channel);

            string? decryptedData = null;

            if (!string.IsNullOrEmpty(config?.ConfigurationData))
            {
                try
                {
                    decryptedData = _protector.Unprotect(config.ConfigurationData);
                }
                catch
                {
                    decryptedData = null;
                }
            }

            return new UpdateNotificationChannelRequestDto(
                config?.IsEnabled ?? false,
                decryptedData
            );
        }

        public async Task SendNotificationAsync(Guid userId, string message, string type)
        {
            var settings = await _uow.Repository<UserNotificationSettings>().AsQueryable()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            bool shouldSend = true;

            if (settings != null)
            {
                shouldSend = type switch
                {
                    "FriendRequest" => settings.FriendRequest,
                    "FriendRequestAccepted" => settings.FriendRequestAccepted,
                    "FriendRequestRejected" => settings.FriendRequestRejected,
                    "ProfileComment" => settings.ProfileComment,
                    "BigSale" => settings.BigSale,
                    "WishlistDiscount" => settings.WishlistDiscount,
                    "ChatMessage" => settings.ChatMessageNotification,
                    _ => true
                };
            }

            if (!shouldSend) return;

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Repository<Notification>().AddAsync(notification);
            await _uow.SaveChangesAsync();
        }

        public async Task<PagedResultDto<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page, int pageSize)
        {
            var query = _uow.Repository<Notification>().AsQueryable()
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }).ToListAsync();

            return new PagedResultDto<NotificationDto>(items, totalCount, page, pageSize);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _uow.Repository<Notification>().AsQueryable()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var repo = _uow.Repository<Notification>();
            var notification = await repo.GetByIdAsync(notificationId);

            if (notification != null && notification.UserId == userId)
            {
                notification.IsRead = true;
                repo.Update(notification);
                await _uow.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var repo = _uow.Repository<Notification>();
            var unreadNotifications = await repo.AsQueryable()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    repo.Update(notification);
                }
                await _uow.SaveChangesAsync();
            }
        }
    }
}