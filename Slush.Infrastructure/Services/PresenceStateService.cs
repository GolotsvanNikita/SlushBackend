using System.Collections.Concurrent;
using Slush.Application.DTOs.Monitoring;

namespace Slush.Infrastructure.Services;

public class UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Status { get; set; } = "Online";
    public string CurrentGame { get; set; } = string.Empty;
    public GeoPoint Location { get; set; } = new(0, 0, "Unknown", "Unknown");
}

public class PresenceStateService
{
    public ConcurrentDictionary<string, UserSession> ActiveConnections { get; } = new();
}