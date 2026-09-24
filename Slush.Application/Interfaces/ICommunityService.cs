using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Community;
using Slush.Domain.Enums;

namespace Slush.Application.Interfaces;

public interface ICommunityService
{
    Task<PagedResultDto<CommunityPostDto>> GetGamePostsAsync(
        string gameId,
        CommunityPostType? postType,
        PostSortOption sortOption,
        int page,
        int pageSize,
        Guid? currentUserId = null);

    Task<CommunityPostDto> CreatePostAsync(Guid userId, CreateCommunityPostDto request);
    Task ToggleLikeAsync(Guid userId, Guid postId);
    Task<PostCommentDto> AddCommentAsync(Guid userId, Guid postId, CreateCommentDto request);
    Task<PagedResultDto<PostCommentDto>> GetPostCommentsAsync(Guid postId, int page, int pageSize);
    Task UpdatePostAsync(Guid userId, Guid postId, UpdateCommunityPostDto request);
    Task DeletePostAsync(Guid userId, Guid postId);

    Task UpdateCommentAsync(Guid userId, Guid commentId, UpdateCommentDto request);
    Task DeleteCommentAsync(Guid userId, Guid commentId);
    Task<CommunityTabCountsDto> GetGameTabCountsAsync(string gameId, Guid? currentUserId = null);
    Task ToggleSubscribeAsync(Guid userId, string gameId);
}