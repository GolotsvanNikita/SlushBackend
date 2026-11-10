using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;
using Slush.Application.DTOs.Store;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;

namespace Slush.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _uow;

    public ProfileService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        return (Math.Max(1, page), Math.Clamp(pageSize, 1, 50));
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var normalized = username.Trim().ToLower();

        var user = await _uow.Repository<User>().AsQueryable()
            .Include(u => u.UserBadges)
                .ThenInclude(ub => ub.Badge)
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Username.ToLower() == normalized);

        if (user == null) return null;

        var reviewsCount = await _uow.Repository<GameReview>().AsQueryable()
            .CountAsync(r => r.UserId == user.Id);

        var wishlistCount = await _uow.Repository<WishlistItem>().AsQueryable()
            .CountAsync(w => w.UserId == user.Id);

        var communityRepo = _uow.Repository<CommunityPost>().AsQueryable()
            .Where(p => p.UserId == user.Id);

        var discussionsCount = await communityRepo.CountAsync(p => p.PostType == CommunityPostType.Discussion);
        var screenshotsCount = await communityRepo.CountAsync(p => p.PostType == CommunityPostType.Screenshot);
        var videosCount = await communityRepo.CountAsync(p => p.PostType == CommunityPostType.Video);
        var guidesCount = await communityRepo.CountAsync(p => p.PostType == CommunityPostType.Guide);

        var friendsQuery = _uow.Repository<FriendRequest>().AsQueryable()
            .Include(r => r.Sender)
            .Include(r => r.Receiver)
            .Where(r => (r.SenderId == user.Id && !r.Receiver.IsDeleted ||
                         r.ReceiverId == user.Id && !r.Sender.IsDeleted)
                        && r.Status == FriendRequestStatus.Accepted);

        var friendsCount = await friendsQuery.CountAsync();

        var friendsList = await friendsQuery
            .Select(r => r.SenderId == user.Id ? r.Receiver : r.Sender)
            .Take(6)
            .Select(u => new FriendDto
            {
                Id = u.Id.ToString(),
                Username = u.Username,
                AvatarUrl = u.AvatarUrl,
                Level = u.Level,
                IsOnline = u.LastSeenAt.HasValue &&
                           u.LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-2),
                LastSeenAt = u.LastSeenAt,
                CurrentGameId = (u.LastSeenAt.HasValue && u.LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-2))
                                ? u.CurrentGameId
                                : null,
                CurrentGameTitle = (u.LastSeenAt.HasValue && u.LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-2))
                                ? u.CurrentGameTitle
                                : null
            }).ToListAsync();

        return new UserProfileDto
        {
            Id = user.Id.ToString(),
            Username = user.Username,
            IsOnline = user.LastSeenAt.HasValue && user.LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-2),
            LastSeenAt = user.LastSeenAt,
            Status = user.LastSeenAt.HasValue && user.LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-2) ? "online" : "offline",
            Bio = string.IsNullOrWhiteSpace(user.Bio) ? "No bio yet." : user.Bio,
            AvatarUrl = user.AvatarUrl,
            CoverUrl = user.CoverUrl,
            Level = user.Level == 0 ? 1 : user.Level,
            CurrentXp = user.CurrentXp,
            MaxXp = user.Level == 0 ? 1000 : user.Level * 1000 + 500,

            Counters = new ProfileCountersDto
            {
                Badges = user.UserBadges.Count,
                Games = await _uow.Repository<UserGame>().AsQueryable().CountAsync(g => g.UserId == user.Id),
                Wishlist = wishlistCount,
                Reviews = reviewsCount,
                Guides = guidesCount,
                Friends = friendsCount,
                Discussions = discussionsCount,
                Screenshots = screenshotsCount,
                Videos = videosCount
            },

            Badges = user.UserBadges.Select(ub => new BadgeDto
            {
                Id = ub.Badge.Id.ToString(),
                Title = ub.Badge.Title,
                Description = ub.Badge.Description,
                Points = ub.Badge.XpReward,
                ImageUrl = ub.Badge.ImageUrl,
                EarnedAt = ub.EarnedAt.ToString("dd.MM.yyyy")
            }).ToList(),

            Friends = friendsList
        };
    }

    public async Task<PagedResultDto<StoreItemDto>> GetProfileWishlistAsync(string username, int page, int pageSize)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var normalized = username.Trim().ToLower();

        var query = _uow.Repository<WishlistItem>().AsQueryable()
            .Where(w => !w.User.IsDeleted && w.User.Username.ToLower() == normalized)
            .OrderByDescending(w => w.AddedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new StoreItemDto
            {
                GameId = w.GameId,
                Title = w.Title,
                ImageUrl = w.ImageUrl,
                Price = w.Price,
                OldPrice = w.OldPrice,
                DiscountPercent = w.DiscountPercent,
                AddedAt = w.AddedAt
            })
            .ToListAsync();

        return new PagedResultDto<StoreItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileCommentDto>> GetProfileCommentsAsync(string username, int page, int pageSize)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var normalized = username.Trim().ToLower();

        var query = _uow.Repository<ProfileComment>().AsQueryable()
            .Include(c => c.Author)
            .Where(c => !c.ProfileUser.IsDeleted && c.ProfileUser.Username.ToLower() == normalized)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ProfileCommentDto
            {
                Id = c.Id.ToString(),
                AuthorUsername = c.Author.Username,
                AuthorAvatarUrl = c.Author.AvatarUrl,
                Text = c.Text,
                CreatedAt = c.CreatedAt.ToString("dd.MM.yyyy")
            })
            .ToListAsync();

        return new PagedResultDto<ProfileCommentDto>(comments, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileReviewDto>> GetProfileReviewsAsync(string username, int page, int pageSize)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var normalized = username.Trim().ToLower();

        var query = _uow.Repository<GameReview>().AsQueryable()
            .Include(r => r.User)
            .Where(r => !r.User.IsDeleted && r.User.Username.ToLower() == normalized)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProfileReviewDto
            {
                Id = r.Id.ToString(),
                GameId = r.GameId,
                GameTitle = r.GameTitle,
                GameBannerUrl = r.GameBannerUrl,
                Rating = r.Score,
                Text = r.Text,
                LikesCount = 0,
                CommentsCount = 0,
                CreatedAt = r.CreatedAt.ToString("dd.MM.yyyy")
            })
            .ToListAsync();

        return new PagedResultDto<ProfileReviewDto>(reviews, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileGameDto>> GetProfileGamesAsync(string username, int page, int pageSize)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var normalized = username.Trim().ToLower();

        var query = _uow.Repository<UserGame>().AsQueryable()
            .Where(g => !g.User.IsDeleted && g.User.Username.ToLower() == normalized)
            .OrderByDescending(g => g.AcquiredAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new ProfileGameDto
            {
                Id = g.GameId,
                Title = g.Title,
                ImageUrl = g.ImageUrl,
                Price = g.Price
            }).ToListAsync();

        return new PagedResultDto<ProfileGameDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfilePostDto>> GetProfilePostsAsync(
        string username,
        Guid? viewerUserId,
        string? type,
        string? sort,
        int page,
        int pageSize)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var normalized = username.Trim().ToLower();

        var query = _uow.Repository<CommunityPost>().AsQueryable()
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Where(p => !p.User.IsDeleted && p.User.Username.ToLower() == normalized);

        if (!string.IsNullOrWhiteSpace(type) &&
            Enum.TryParse<CommunityPostType>(type.Trim(), true, out var parsedType))
        {
            query = query.Where(p => p.PostType == parsedType);
        }
        else if (string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(p => p.PostType == CommunityPostType.Discussion);
        }

        if (string.Equals(sort?.Trim(), "ByRating", StringComparison.OrdinalIgnoreCase))
        {
            query = query
                .OrderByDescending(p => p.LikesCount)
                .ThenByDescending(p => p.CreatedAt);
        }
        else
        {
            query = query.OrderByDescending(p => p.CreatedAt);
        }

        var totalCount = await query.CountAsync();
        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = posts.Select(p =>
        {
            var content = p.Content ?? string.Empty;
            var shortDesc = !string.IsNullOrWhiteSpace(p.ShortDescription)
                ? p.ShortDescription
                : (content.Length > 150 ? content[..150] + "..." : content);

            return new ProfilePostDto
            {
                Id = p.Id.ToString(),
                AuthorUsername = p.User.Username,
                AuthorAvatarUrl = p.User.AvatarUrl,
                PostType = p.PostType.ToString(),
                IsLiked = viewerUserId.HasValue && p.Likes.Any(l => l.UserId == viewerUserId.Value),
                Title = p.Title ?? string.Empty,
                Content = content,
                ShortDescription = shortDesc,
                MediaUrl = p.MediaUrl,
                Text = content,
                ImageUrl = p.MediaUrl ?? string.Empty,
                LikesCount = p.LikesCount,
                CommentsCount = p.CommentsCount,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd")
            };
        }).ToList();

        return new PagedResultDto<ProfilePostDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileScreenshotDto>> GetProfileScreenshotsAsync(string username, int page, int pageSize)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var normalized = username.Trim().ToLower();

        var query = _uow.Repository<CommunityPost>().AsQueryable()
            .Where(p => !p.User.IsDeleted && p.User.Username.ToLower() == normalized && p.PostType == CommunityPostType.Screenshot)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProfileScreenshotDto
            {
                Id = p.Id.ToString(),
                ImageUrl = p.MediaUrl ?? string.Empty,
                GameTitle = $"Game ID: {p.GameId}",
                GameId = p.GameId,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd")
            }).ToListAsync();

        return new PagedResultDto<ProfileScreenshotDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileVideoDto>> GetProfileVideosAsync(string username, int page, int pageSize)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var normalized = username.Trim().ToLower();

        var query = _uow.Repository<CommunityPost>().AsQueryable()
            .Where(p => !p.User.IsDeleted && p.User.Username.ToLower() == normalized && p.PostType == CommunityPostType.Video)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProfileVideoDto
            {
                Id = p.Id.ToString(),
                VideoUrl = p.MediaUrl ?? string.Empty,
                ThumbnailUrl = string.Empty,
                Title = p.Title ?? p.Content ?? "Video",
                GameTitle = $"Game ID: {p.GameId}",
                GameId = p.GameId,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd")
            }).ToListAsync();

        return new PagedResultDto<ProfileVideoDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileGuideDto>> GetProfileGuidesAsync(string username, int page, int pageSize)
    {
        (page, pageSize) = NormalizePagination(page, pageSize);
        var normalized = username.Trim().ToLower();

        var query = _uow.Repository<CommunityPost>().AsQueryable()
            .Where(p => !p.User.IsDeleted && p.User.Username.ToLower() == normalized && p.PostType == CommunityPostType.Guide)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProfileGuideDto
            {
                Id = p.Id.ToString(),
                GameId = p.GameId,
                GameTitle = $"Game ID: {p.GameId}",
                GuideTitle = p.Title ?? string.Empty,
                TextSnippet = !string.IsNullOrEmpty(p.ShortDescription)
                    ? p.ShortDescription
                    : (p.Content != null && p.Content.Length > 150 ? p.Content.Substring(0, 150) + "..." : p.Content ?? string.Empty),
                LikesCount = p.LikesCount,
                CommentsCount = p.CommentsCount,
                CreatedAt = p.CreatedAt.ToString("dd.MM.yyyy")
            }).ToListAsync();

        return new PagedResultDto<ProfileGuideDto>(items, totalCount, page, pageSize);
    }
}