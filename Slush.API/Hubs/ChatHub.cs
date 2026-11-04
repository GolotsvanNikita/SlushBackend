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
    private readonly ILibraryService _libraryService;
    private readonly IConfiguration _configuration;

    public ChatHub(IUnitOfWork uow, ILibraryService libraryService, IConfiguration configuration)
    {
        _uow = uow;
        _libraryService = libraryService;
        _configuration = configuration;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(userIdStr, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            int timeoutMinutes = _configuration.GetValue<int>("GameSession:StaleTimeoutMinutes", 3);
            await _libraryService.CleanupStaleSessionsAsync(TimeSpan.FromMinutes(Math.Max(1, timeoutMinutes)));

            var user = await _uow.Repository<User>().GetByIdAsync(userId);
            if (user != null)
            {
                var now = DateTime.UtcNow;
                bool wasOffline = !user.LastSeenAt.HasValue ||
                                  (now - user.LastSeenAt.Value) > TimeSpan.FromMinutes(2);

                user.LastSeenAt = now;
                _uow.Repository<User>().Update(user);
                await _uow.SaveChangesAsync();

                if (wasOffline)
                {
                    var friendIds = await _libraryService.GetAcceptedFriendIdsAsync(userId);

                    if (friendIds.Any())
                    {
                        var groups = friendIds.Select(id => $"user_{id}").ToList();
                        var onlineToast = new FriendPresenceToastDto
                        {
                            UserId = user.Id,
                            Username = user.Username,
                            AvatarUrl = user.AvatarUrl,
                            EventType = "Online",
                            StatusText = "У мережі",
                            Timestamp = now
                        };

                        await Clients.Groups(groups).SendAsync("FriendPresenceChanged", onlineToast);
                    }
                }
            }
        }

        await base.OnConnectedAsync();
    }
}