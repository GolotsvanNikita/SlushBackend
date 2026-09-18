using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Alerts;
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
    }
}