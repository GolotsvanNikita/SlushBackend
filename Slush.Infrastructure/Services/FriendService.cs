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
        var request = await _uow.Repository<FriendRequest>().AsQueryable()
            .Where(r => (r.SenderId == userId && r.ReceiverId == targetUserId) ||
                        (r.SenderId == targetUserId && r.ReceiverId == userId))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (request == null || request.Status == FriendRequestStatus.Cancelled || request.Status == FriendRequestStatus.Rejected)
            return new FriendStatusDto { Status = "None" };

        if (request.Status == FriendRequestStatus.Accepted)
            return new FriendStatusDto { Status = "Friends", RequestId = request.Id };

        if (request.Status == FriendRequestStatus.Pending)
        {
            if (request.SenderId == userId)
                return new FriendStatusDto { Status = "PendingSent", RequestId = request.Id };

            return new FriendStatusDto { Status = "PendingReceived", RequestId = request.Id };
        }

        return new FriendStatusDto { Status = "None" };
    }

    public async Task SendRequestAsync(Guid userId, Guid targetUserId)
    {
        if (userId == targetUserId) throw new Exception("You cannot send a friend request to yourself.");

        var targetUser = await _uow.Repository<User>().GetByIdAsync(targetUserId);
        if (targetUser == null) throw new Exception("User not found.");

        var statusDto = await GetStatusAsync(userId, targetUserId);

        switch (statusDto.Status)
        {
            case "Friends": throw new Exception("Users are already friends.");
            case "PendingSent": throw new Exception("Friend request already sent.");
            case "PendingReceived": throw new Exception("Friend request already received.");
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

        if (request == null || request.ReceiverId != userId || request.Status != FriendRequestStatus.Pending)
            throw new Exception("Invalid friend request.");

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

        if (request == null || request.ReceiverId != userId || request.Status != FriendRequestStatus.Pending)
            throw new Exception("Invalid friend request.");

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

        if (request == null || request.SenderId != userId || request.Status != FriendRequestStatus.Pending)
            throw new Exception("Invalid friend request.");

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

        if (request == null) throw new Exception("Friendship not found.");

        request.Status = FriendRequestStatus.Cancelled;
        request.RespondedAt = DateTime.UtcNow;
        repo.Update(request);
        await _uow.SaveChangesAsync();
    }

    public async Task<PagedResultDto<FriendUserDto>> GetFriendsAsync(Guid userId, int page, int pageSize)
    {
        var query = _uow.Repository<FriendRequest>().AsQueryable()
            .Include(r => r.Sender)
            .Include(r => r.Receiver)
            .Where(r => (r.SenderId == userId || r.ReceiverId == userId) && r.Status == FriendRequestStatus.Accepted)
            .OrderByDescending(r => r.RespondedAt);

        var totalCount = await query.CountAsync();

        var friendsList = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => r.SenderId == userId ? r.Receiver : r.Sender)
            .Select(u => new FriendUserDto
            {
                UserId = u.Id,
                Username = u.Username,
                AvatarUrl = u.AvatarUrl,
                LastSeenAt = u.LastSeenAt
            })
            .ToListAsync();

        foreach (var friend in friendsList)
        {
            friend.IsOnline = friend.LastSeenAt.HasValue &&
                              (DateTime.UtcNow - friend.LastSeenAt.Value) <= TimeSpan.FromMinutes(2);
        }

        return new PagedResultDto<FriendUserDto>(friendsList, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<FriendRequestDto>> GetIncomingRequestsAsync(Guid userId, int page, int pageSize)
    {
        var query = _uow.Repository<FriendRequest>().AsQueryable()
            .Include(r => r.Sender)
            .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var requests = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new FriendRequestDto
            {
                Id = r.Id,
                User = new FriendUserDto { UserId = r.Sender.Id, Username = r.Sender.Username, AvatarUrl = r.Sender.AvatarUrl },
                CreatedAt = r.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToListAsync();

        return new PagedResultDto<FriendRequestDto>(requests, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<FriendRequestDto>> GetOutgoingRequestsAsync(Guid userId, int page, int pageSize)
    {
        var query = _uow.Repository<FriendRequest>().AsQueryable()
            .Include(r => r.Receiver)
            .Where(r => r.SenderId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var requests = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new FriendRequestDto
            {
                Id = r.Id,
                User = new FriendUserDto { UserId = r.Receiver.Id, Username = r.Receiver.Username, AvatarUrl = r.Receiver.AvatarUrl },
                CreatedAt = r.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToListAsync();

        return new PagedResultDto<FriendRequestDto>(requests, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<UserSearchResultDto>> SearchUsersAsync(Guid userId, string query, int page, int pageSize)
    {
        var normalizedQuery = query.ToLower();
        var userQuery = _uow.Repository<User>().AsQueryable()
            .Where(u => u.Id != userId && u.Username.ToLower().Contains(normalizedQuery));

        var totalCount = await userQuery.CountAsync();
        var users = await userQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

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
}