using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Friends;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;

namespace Slush.Infrastructure.Services;

public class FriendService : IFriendService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public FriendService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    public async Task<FriendStatusDto> GetStatusAsync(Guid userId, Guid targetUserId)
    {
        var blockedIds = await GetBlockedUserIdsBothWaysAsync(userId);
        if (blockedIds.Contains(targetUserId))
            return new FriendStatusDto { Status = "None", RequestId = null };

        var request = await _uow.Repository<FriendRequest>().AsQueryable()
            .Where(r => (r.SenderId == userId && r.ReceiverId == targetUserId) ||
                        (r.SenderId == targetUserId && r.ReceiverId == userId))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (request == null || request.Status == FriendRequestStatus.Cancelled || request.Status == FriendRequestStatus.Rejected)
            return new FriendStatusDto { Status = "None", RequestId = null };

        if (request.Status == FriendRequestStatus.Accepted)
            return new FriendStatusDto { Status = "Friends", RequestId = request.Id };

        if (request.Status == FriendRequestStatus.Pending)
        {
            if (request.SenderId == userId)
                return new FriendStatusDto { Status = "PendingSent", RequestId = request.Id };

            return new FriendStatusDto { Status = "PendingReceived", RequestId = request.Id };
        }

        return new FriendStatusDto { Status = "None", RequestId = null };
    }

    public async Task SendRequestAsync(Guid userId, Guid targetUserId)
    {
        if (userId == targetUserId)
            throw new ArgumentException("You cannot send a friend request to yourself.");

        var targetUser = await _uow.Repository<User>().GetByIdAsync(targetUserId);
        if (targetUser == null || targetUser.IsDeleted)
            throw new KeyNotFoundException("User not found.");

        var blockedIds = await GetBlockedUserIdsBothWaysAsync(userId);
        if (blockedIds.Contains(targetUserId))
            throw new InvalidOperationException("Cannot send friend request due to block restrictions.");

        var statusDto = await GetStatusAsync(userId, targetUserId);

        switch (statusDto.Status)
        {
            case "Friends": throw new InvalidOperationException("Users are already friends.");
            case "PendingSent": throw new InvalidOperationException("Friend request already sent.");
            case "PendingReceived": throw new InvalidOperationException("Friend request already received.");
        }

        var request = new FriendRequest
        {
            SenderId = userId,
            ReceiverId = targetUserId,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Repository<FriendRequest>().AddAsync(request);
        await _uow.SaveChangesAsync();

        var sender = await _uow.Repository<User>().GetByIdAsync(userId);
        await _notificationService.SendNotificationAsync(targetUserId, $"{sender!.Username} хоче додати вас у друзі.", "FriendRequest");
    }

    public async Task AcceptRequestAsync(Guid userId, Guid requestId)
    {
        var repo = _uow.Repository<FriendRequest>();
        var request = await repo.GetByIdAsync(requestId);

        if (request == null)
            throw new KeyNotFoundException("Friend request not found.");

        if (request.ReceiverId != userId || request.Status != FriendRequestStatus.Pending)
            throw new InvalidOperationException("Invalid friend request.");

        request.Status = FriendRequestStatus.Accepted;
        request.RespondedAt = DateTime.UtcNow;
        repo.Update(request);
        await _uow.SaveChangesAsync();

        var receiver = await _uow.Repository<User>().GetByIdAsync(userId);
        await _notificationService.SendNotificationAsync(request.SenderId, $"{receiver!.Username} прийняв ваш запит на дружбу.", "FriendRequestAccepted");
    }

    public async Task RejectRequestAsync(Guid userId, Guid requestId)
    {
        var repo = _uow.Repository<FriendRequest>();
        var request = await repo.GetByIdAsync(requestId);

        if (request == null)
            throw new KeyNotFoundException("Friend request not found.");

        if (request.ReceiverId != userId || request.Status != FriendRequestStatus.Pending)
            throw new InvalidOperationException("Invalid friend request.");

        request.Status = FriendRequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;
        repo.Update(request);
        await _uow.SaveChangesAsync();

        var receiver = await _uow.Repository<User>().GetByIdAsync(userId);
        await _notificationService.SendNotificationAsync(request.SenderId, $"{receiver!.Username} відхилив ваш запит на дружбу.", "FriendRequestRejected");
    }

    public async Task CancelRequestAsync(Guid userId, Guid requestId)
    {
        var repo = _uow.Repository<FriendRequest>();
        var request = await repo.GetByIdAsync(requestId);

        if (request == null)
            throw new KeyNotFoundException("Friend request not found.");

        if (request.SenderId != userId || request.Status != FriendRequestStatus.Pending)
            throw new InvalidOperationException("Invalid friend request.");

        request.Status = FriendRequestStatus.Cancelled;
        request.RespondedAt = DateTime.UtcNow;
        repo.Update(request);
        await _uow.SaveChangesAsync();
    }

    public async Task RemoveFriendAsync(Guid userId, Guid friendUserId)
    {
        var repo = _uow.Repository<FriendRequest>();
        var request = await repo.AsQueryable()
            .Where(r => ((r.SenderId == userId && r.ReceiverId == friendUserId) ||
                         (r.SenderId == friendUserId && r.ReceiverId == userId))
                        && r.Status == FriendRequestStatus.Accepted)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (request == null)
            throw new KeyNotFoundException("Friendship not found.");

        request.Status = FriendRequestStatus.Cancelled;
        request.RespondedAt = DateTime.UtcNow;
        repo.Update(request);
        await _uow.SaveChangesAsync();
    }

    public async Task<PagedResultDto<FriendUserDto>> GetFriendsAsync(Guid userId, string? filter, int page, int pageSize)
    {
        var blockedIds = await GetBlockedUserIdsBothWaysAsync(userId);

        var requests = await _uow.Repository<FriendRequest>().AsQueryable()
            .Include(r => r.Sender)
            .Include(r => r.Receiver)
            .Where(r => (r.SenderId == userId || r.ReceiverId == userId)
                        && r.Status == FriendRequestStatus.Accepted)
            .ToListAsync();

        var now = DateTime.UtcNow;

        var allFriendsQuery = requests
            .Select(r => r.SenderId == userId ? r.Receiver : r.Sender)
            .Where(u => !u.IsDeleted && !blockedIds.Contains(u.Id))
            .DistinctBy(u => u.Id)
            .Select(friend =>
            {
                bool isOnline = friend.LastSeenAt.HasValue &&
                                (now - friend.LastSeenAt.Value) <= TimeSpan.FromMinutes(3);

                return new FriendUserDto
                {
                    UserId = friend.Id,
                    Username = friend.Username,
                    AvatarUrl = friend.AvatarUrl,
                    Level = friend.Level,
                    LastSeenAt = friend.LastSeenAt,
                    IsOnline = isOnline,
                    CurrentGameId = isOnline ? friend.CurrentGameId : null,
                    CurrentGameTitle = isOnline ? friend.CurrentGameTitle : null
                };
            });

        if (string.Equals(filter?.Trim(), "online", StringComparison.OrdinalIgnoreCase))
        {
            allFriendsQuery = allFriendsQuery.Where(f => f.IsOnline);
        }

        var allFriends = allFriendsQuery
            .OrderByDescending(f => f.IsOnline)
            .ThenBy(f => f.Username)
            .ToList();

        var totalCount = allFriends.Count;
        var items = allFriends
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResultDto<FriendUserDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<FriendRequestDto>> GetIncomingRequestsAsync(Guid userId, int page, int pageSize)
    {
        var blockedIds = await GetBlockedUserIdsBothWaysAsync(userId);

        var query = _uow.Repository<FriendRequest>().AsQueryable()
            .Include(r => r.Sender)
            .Where(r => r.ReceiverId == userId
                        && r.Status == FriendRequestStatus.Pending
                        && !blockedIds.Contains(r.SenderId))
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var requests = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new FriendRequestDto
            {
                Id = r.Id,
                User = new FriendUserDto
                {
                    UserId = r.Sender.Id,
                    Username = r.Sender.Username,
                    AvatarUrl = r.Sender.AvatarUrl,
                    Level = r.Sender.Level
                },
                CreatedAt = r.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToListAsync();

        return new PagedResultDto<FriendRequestDto>(requests, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<FriendRequestDto>> GetOutgoingRequestsAsync(Guid userId, int page, int pageSize)
    {
        var blockedIds = await GetBlockedUserIdsBothWaysAsync(userId);

        var query = _uow.Repository<FriendRequest>().AsQueryable()
            .Include(r => r.Receiver)
            .Where(r => r.SenderId == userId
                        && r.Status == FriendRequestStatus.Pending
                        && !blockedIds.Contains(r.ReceiverId))
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var requests = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new FriendRequestDto
            {
                Id = r.Id,
                User = new FriendUserDto
                {
                    UserId = r.Receiver.Id,
                    Username = r.Receiver.Username,
                    AvatarUrl = r.Receiver.AvatarUrl,
                    Level = r.Receiver.Level
                },
                CreatedAt = r.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToListAsync();

        return new PagedResultDto<FriendRequestDto>(requests, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<UserSearchResultDto>> SearchUsersAsync(
        Guid userId, string? query, string? gameId, int page, int pageSize)
    {
        var blockedIds = await GetBlockedUserIdsBothWaysAsync(userId);

        var userQuery = _uow.Repository<User>().AsQueryable()
            .Where(u => u.Id != userId && !u.IsDeleted && !blockedIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim().ToLower();
            userQuery = userQuery.Where(u => u.Username.ToLower().Contains(normalizedQuery));
        }

        if (!string.IsNullOrWhiteSpace(gameId))
        {
            var trimmedGameId = gameId.Trim();
            var usersWithGame = _uow.Repository<UserGame>().AsQueryable()
                .Where(ug => ug.GameId == trimmedGameId)
                .Select(ug => ug.UserId);

            userQuery = userQuery.Where(u => usersWithGame.Contains(u.Id));
        }

        var totalCount = await userQuery.CountAsync();
        var users = await userQuery
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<UserSearchResultDto>();
        foreach (var user in users)
        {
            var status = await GetStatusAsync(userId, user.Id);
            result.Add(new UserSearchResultDto
            {
                UserId = user.Id,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                FriendStatus = status.Status
            });
        }

        return new PagedResultDto<UserSearchResultDto>(result, totalCount, page, pageSize);
    }

    public async Task<List<BlockedUserDto>> GetBlockedUsersAsync(Guid userId)
    {
        var settings = await _uow.Repository<ChatConversationSetting>().AsQueryable()
            .Where(s => s.UserId == userId && s.IsBlocked)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

        if (!settings.Any()) return [];

        var blockedUserIds = settings.Select(s => s.FriendId).ToList();
        var users = await _uow.Repository<User>().AsQueryable()
            .Where(u => blockedUserIds.Contains(u.Id) && !u.IsDeleted)
            .ToDictionaryAsync(u => u.Id);

        return settings
            .Where(s => users.ContainsKey(s.FriendId))
            .Select(s => new BlockedUserDto
            {
                UserId = s.FriendId,
                Username = users[s.FriendId].Username,
                AvatarUrl = users[s.FriendId].AvatarUrl,
                BlockedAt = s.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            })
            .ToList();
    }

    public async Task BlockUserAsync(Guid userId, Guid targetUserId)
    {
        if (userId == targetUserId)
            throw new ArgumentException("You cannot block yourself.");

        var targetUser = await _uow.Repository<User>().GetByIdAsync(targetUserId);
        if (targetUser == null || targetUser.IsDeleted)
            throw new KeyNotFoundException("User not found.");

        var settingRepo = _uow.Repository<ChatConversationSetting>();
        var setting = await settingRepo.AsQueryable()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.FriendId == targetUserId);

        if (setting != null && setting.IsBlocked)
            throw new InvalidOperationException("User is already blocked.");

        var now = DateTime.UtcNow;

        if (setting == null)
        {
            setting = new ChatConversationSetting
            {
                UserId = userId,
                FriendId = targetUserId,
                NotificationsEnabled = false,
                IsBlocked = true,
                UpdatedAt = now
            };
            await settingRepo.AddAsync(setting);
        }
        else
        {
            setting.IsBlocked = true;
            setting.NotificationsEnabled = false;
            setting.UpdatedAt = now;
            settingRepo.Update(setting);
        }

        var friendRepo = _uow.Repository<FriendRequest>();
        var activeRelations = await friendRepo.AsQueryable()
            .Where(r => ((r.SenderId == userId && r.ReceiverId == targetUserId) ||
                         (r.SenderId == targetUserId && r.ReceiverId == userId)) &&
                        (r.Status == FriendRequestStatus.Accepted || r.Status == FriendRequestStatus.Pending))
            .ToListAsync();

        foreach (var rel in activeRelations)
        {
            rel.Status = FriendRequestStatus.Cancelled;
            rel.RespondedAt = now;
            friendRepo.Update(rel);
        }

        await _uow.SaveChangesAsync();
    }

    public async Task UnblockUserAsync(Guid userId, Guid targetUserId)
    {
        var settingRepo = _uow.Repository<ChatConversationSetting>();
        var setting = await settingRepo.AsQueryable()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.FriendId == targetUserId && s.IsBlocked);

        if (setting == null)
            throw new KeyNotFoundException("Block record not found.");

        setting.IsBlocked = false;
        setting.UpdatedAt = DateTime.UtcNow;
        settingRepo.Update(setting);

        await _uow.SaveChangesAsync();
    }

    public async Task<PagedResultDto<FriendActivityDto>> GetFriendsActivityAsync(Guid userId, int page, int pageSize)
    {
        var blockedIds = await GetBlockedUserIdsBothWaysAsync(userId);

        var friendIds = await _uow.Repository<FriendRequest>().AsQueryable()
            .Where(r => (r.SenderId == userId || r.ReceiverId == userId)
                        && r.Status == FriendRequestStatus.Accepted)
            .Select(r => r.SenderId == userId ? r.ReceiverId : r.SenderId)
            .Distinct()
            .ToListAsync();

        friendIds = friendIds.Where(id => !blockedIds.Contains(id)).ToList();

        if (!friendIds.Any())
            return new PagedResultDto<FriendActivityDto>([], 0, page, pageSize);

        var purchases = await _uow.Repository<UserGame>().AsQueryable()
            .Include(ug => ug.User)
            .Where(ug => friendIds.Contains(ug.UserId))
            .OrderByDescending(ug => ug.AcquiredAt)
            .Take(100)
            .Select(ug => new
            {
                Id = $"purchase_{ug.Id}",
                ug.UserId,
                ug.User.Username,
                ug.User.AvatarUrl,
                ActivityType = "GamePurchased",
                GameId = (string?)ug.GameId,
                GameTitle = (string?)ug.Title,
                MediaUrl = (string?)(ug.HeaderImage ?? ug.ImageUrl),
                CreatedAt = ug.AcquiredAt
            })
            .ToListAsync();

        var wishlists = await _uow.Repository<WishlistItem>().AsQueryable()
            .Include(w => w.User)
            .Where(w => friendIds.Contains(w.UserId))
            .OrderByDescending(w => w.AddedAt)
            .Take(100)
            .Select(w => new
            {
                Id = $"wishlist_{w.Id}",
                w.UserId,
                w.User.Username,
                w.User.AvatarUrl,
                ActivityType = "WishlistAdded",
                GameId = (string?)w.GameId,
                GameTitle = (string?)w.Title,
                MediaUrl = (string?)w.ImageUrl,
                CreatedAt = w.AddedAt
            })
            .ToListAsync();

        var screenshots = await _uow.Repository<CommunityPost>().AsQueryable()
            .Include(p => p.User)
            .Where(p => friendIds.Contains(p.UserId) && p.PostType == CommunityPostType.Screenshot)
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .Select(p => new
            {
                Id = p.Id.ToString(),
                p.UserId,
                p.User.Username,
                p.User.AvatarUrl,
                ActivityType = "ScreenshotPublished",
                GameId = p.GameId,
                GameTitle = p.Title,
                MediaUrl = p.MediaUrl,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var combined = purchases
            .Concat(wishlists)
            .Concat(screenshots)
            .OrderByDescending(a => a.CreatedAt)
            .ToList();

        var totalCount = combined.Count;
        var items = combined
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new FriendActivityDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Username = a.Username,
                AvatarUrl = a.AvatarUrl,
                ActivityType = a.ActivityType,
                GameId = a.GameId,
                GameTitle = a.GameTitle,
                MediaUrl = a.MediaUrl,
                CreatedAt = a.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            })
            .ToList();

        return new PagedResultDto<FriendActivityDto>(items, totalCount, page, pageSize);
    }

    private async Task<HashSet<Guid>> GetBlockedUserIdsBothWaysAsync(Guid userId)
    {
        var blocks = await _uow.Repository<ChatConversationSetting>().AsQueryable()
            .Where(s => (s.UserId == userId || s.FriendId == userId) && s.IsBlocked)
            .Select(s => s.UserId == userId ? s.FriendId : s.UserId)
            .ToListAsync();

        return blocks.ToHashSet();
    }
}