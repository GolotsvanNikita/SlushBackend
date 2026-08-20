using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Monitoring
{
    public record OnlineStatusDto(
        string SteamId,
        string Nickname,
        string Status,
        string CurrentGame,
        double PlaytimeHours,
        double Lat,
        double Lng,
        string City,
        string Country
    );
}
