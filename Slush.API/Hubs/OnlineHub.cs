using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Slush.Infrastructure.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Slush.API.Hubs;

[Authorize]
public class OnlineHub : Hub
{
    private readonly PresenceStateService _state;

    public OnlineHub(PresenceStateService state)
    {
        _state = state;
    }

    public async Task SetGameStatus(string status, string game)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var nickname = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Player";

        if (userId != null && _state.ActiveConnections.TryGetValue(Context.ConnectionId, out var session))
        {
            session.Status = status;
            session.CurrentGame = game;
            session.UserId = userId;
            session.Nickname = nickname;

            await Clients.Others.SendAsync("OnStatusChanged", userId, status, game);
        }
    }
}