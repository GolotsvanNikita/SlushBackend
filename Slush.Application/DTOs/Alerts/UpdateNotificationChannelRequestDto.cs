using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Alerts
{
    public record UpdateNotificationChannelRequestDto(
        bool IsEnabled,
        string? ConfigurationData
    );
}
