using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Library;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;
using System.Security.Claims;

namespace Slush.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _uow;

    public ChatHub(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdStr, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            var user = await _uow.Repository<User>().GetByIdAsync(userId);
            if (user != null)
            {
                bool wasOffline = !user.LastSeenAt.HasValue ||
                                  (DateTime.UtcNow - user.LastSeenAt.Value) > TimeSpan.FromMinutes(2);

                user.LastSeenAt = DateTime.UtcNow;
                _uow.Repository<User>().Update(user);
                await _uow.SaveChangesAsync();

                if (wasOffline)
                {
                    var friendIds = await _uow.Repository<FriendRequest>().AsQueryable()
                        .Where(r => (r.SenderId == userId || r.ReceiverId == userId)
                                    && r.Status == FriendRequestStatus.Accepted)
                        .Select(r => r.SenderId == userId ? r.ReceiverId : r.SenderId)
                        .ToListAsync();

                    if (friendIds.Any())
                    {
                        var groups = friendIds.Select(id => $"user_{id}").ToList();
                        var toast = new FriendPresenceToastDto
                        {
                            UserId = user.Id,
                            Username = user.Username,
                            AvatarUrl = user.AvatarUrl,
                            EventType = !string.IsNullOrEmpty(user.CurrentGameTitle) ? "InGame" : "Online",
                            GameId = user.CurrentGameId,
                            GameTitle = user.CurrentGameTitle,
                            StatusText = !string.IsNullOrEmpty(user.CurrentGameTitle)
                                ? $"Зараз грає в {user.CurrentGameTitle}"
                                : "Зайшов у мережу"
                        };

                        await Clients.Groups(groups).SendAsync("FriendPresenceChanged", toast);
                    }
                }
            }
        }

        await base.OnConnectedAsync();
    }
}