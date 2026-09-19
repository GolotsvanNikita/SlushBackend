using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;

namespace Slush.Application.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto?> GetUserProfileAsync(string username);
    Task<PagedResultDto<ProfileCommentDto>> GetProfileCommentsAsync(string username, int page, int pageSize);
    Task<PagedResultDto<ProfileReviewDto>> GetProfileReviewsAsync(string username, int page, int pageSize);
    Task<PagedResultDto<ProfileGuideDto>> GetProfileGuidesAsync(string username, int page, int pageSize);
}