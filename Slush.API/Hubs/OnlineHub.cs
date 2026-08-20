using Microsoft.AspNetCore.SignalR;

namespace Slush.API.Hubs;

public class OnlineHub : Hub
{
    public async Task BroadcastStatusChange(string steamId, string status, string game)
    {
        await Clients.Others.SendAsync("OnStatusChanged", steamId, status, game);
    }
}