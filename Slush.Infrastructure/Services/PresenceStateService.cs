using System.Collections.Concurrent;
using Slush.Application.DTOs.Monitoring;

namespace Slush.Infrastructure.Services;

public class PresenceStateService
{
    public ConcurrentDictionary<string, GeoPoint> ActiveConnections { get; } = new();
}