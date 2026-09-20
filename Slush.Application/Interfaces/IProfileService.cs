using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;

namespace Slush.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto?> GetUserProfileAsync(string username);
    Task<PagedResultDto<ProfileCommentDto>> GetProfileCommentsAsync(string username, int page, int pageSize);
    Task<PagedResultDto<ProfileReviewDto>> GetProfileReviewsAsync(string username, int page, int pageSize);
    Task<PagedResultDto<ProfileGuideDto>> GetProfileGuidesAsync(string username, int page, int pageSize);
    Task<PagedResultDto<ProfileGameDto>> GetProfileGamesAsync(string username, int page, int pageSize);
    Task<PagedResultDto<ProfilePostDto>> GetProfilePostsAsync(string username, int page, int pageSize);
    Task<PagedResultDto<ProfileScreenshotDto>> GetProfileScreenshotsAsync(string username, int page, int pageSize);
    Task<PagedResultDto<ProfileVideoDto>> GetProfileVideosAsync(string username, int page, int pageSize);
}