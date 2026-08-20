using Microsoft.AspNetCore.SignalR;
using Slush.Infrastructure.Services;

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

        var geo = _geo.GetLocation(ip);

        _state.ActiveConnections.TryAdd(Context.ConnectionId, geo);

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