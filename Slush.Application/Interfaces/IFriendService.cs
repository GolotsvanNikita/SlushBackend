using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Friends;

namespace Slush.Application.Interfaces;

public interface IFriendService
{
    Task<PagedResultDto<FriendUserDto>> GetFriendsAsync(Guid userId, int page, int pageSize);
    Task<PagedResultDto<FriendRequestDto>> GetIncomingRequestsAsync(Guid userId, int page, int pageSize);
    Task<PagedResultDto<FriendRequestDto>> GetOutgoingRequestsAsync(Guid userId, int page, int pageSize);
    Task<FriendStatusDto> GetStatusAsync(Guid userId, Guid targetUserId);
    Task SendRequestAsync(Guid userId, Guid targetUserId);
    Task AcceptRequestAsync(Guid userId, Guid requestId);
    Task RejectRequestAsync(Guid userId, Guid requestId);
    Task CancelRequestAsync(Guid userId, Guid requestId);
    Task RemoveFriendAsync(Guid userId, Guid friendUserId);
    Task<PagedResultDto<UserSearchResultDto>> SearchUsersAsync(Guid userId, string query, int page, int pageSize);
}