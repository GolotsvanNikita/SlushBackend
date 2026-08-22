using Microsoft.AspNetCore.SignalR;
using Slush.Infrastructure.Services;
using System.Security.Claims;

namespace Slush.API.Hubs;

public class GlobeHub(IGeoLocationService geo, PresenceStateService state) : Hub
{
    private readonly IGeoLocationService _geo = geo;
    private readonly PresenceStateService _state = state;

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var ip = httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                 ?? httpContext?.Connection.RemoteIpAddress?.ToString();

        var location = _geo.GetLocation(ip);

        var session = new UserSession
        {
            Location = location,
            UserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            Nickname = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Guest",
            Status = "Online"
        };

        _state.ActiveConnections.TryAdd(Context.ConnectionId, session);

        await Clients.All.SendAsync("UsersUpdated", _state.ActiveConnections.Values);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _state.ActiveConnections.TryRemove(Context.ConnectionId, out _);

        await Clients.All.SendAsync("UsersUpdated", _state.ActiveConnections.Values);
        await base.OnDisconnectedAsync(exception);
    }
}