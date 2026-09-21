using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Common;
using Slush.Application.DTOs.Profile;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _uow;

    public ProfileService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string username)
    {
        var user = await _uow.Repository<User>().AsQueryable()
            .Include(u => u.UserBadges)
                .ThenInclude(ub => ub.Badge)
            .Include(u => u.Guides)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null) return null;

        var reviewsCount = await _uow.Repository<GameReview>().AsQueryable()
            .CountAsync(r => r.UserId == user.Id);

        var wishlistCount = await _uow.Repository<WishlistItem>().AsQueryable()
            .CountAsync(w => w.UserId == user.Id);

        var friendsQuery = _uow.Repository<Friendship>().AsQueryable()
            .Where(f => (f.UserId == user.Id || f.FriendId == user.Id) && f.Status == FriendshipStatus.Accepted);

        var friendsCount = await friendsQuery.CountAsync();

        var friendsList = await friendsQuery
            .Select(f => f.UserId == user.Id ? f.Friend : f.User)
            .Take(6)
            .Select(u => new FriendDto
            {
                Id = u.Id.ToString(),
                Username = u.Username,
                AvatarUrl = u.AvatarUrl,
                Level = u.Level
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
                Games = 0,
                Wishlist = wishlistCount,
                Reviews = reviewsCount,
                Guides = user.Guides.Count,
                Friends = friendsCount,
                Discussions = 0,
                Screenshots = 0,
                Videos = 0
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

    public async Task<PagedResultDto<ProfileCommentDto>> GetProfileCommentsAsync(string username, int page, int pageSize)
    {
        var query = _uow.Repository<ProfileComment>().AsQueryable()
            .Include(c => c.Author)
            .Where(c => c.ProfileUser.Username.ToLower() == username.ToLower())
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
        var query = _uow.Repository<GameReview>().AsQueryable()
            .Include(r => r.User)
            .Where(r => r.User.Username.ToLower() == username.ToLower())
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProfileReviewDto
            {
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

    public async Task<PagedResultDto<ProfileGuideDto>> GetProfileGuidesAsync(string username, int page, int pageSize)
    {
        var query = _uow.Repository<UserGuide>().AsQueryable()
            .Include(g => g.User)
            .Where(g => g.User.Username.ToLower() == username.ToLower())
            .OrderByDescending(g => g.CreatedAt);

        var totalCount = await query.CountAsync();

        var guides = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new ProfileGuideDto
            {
                GameTitle = $"Game ID: {g.GameId}",
                GuideTitle = g.Title,
                TextSnippet = g.Content.Length > 150 ? g.Content.Substring(0, 150) + "..." : g.Content,
                LikesCount = g.LikesCount,
                CommentsCount = 0,
                CreatedAt = g.CreatedAt.ToString("dd.MM.yyyy")
            })
            .ToListAsync();

        return new PagedResultDto<ProfileGuideDto>(guides, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileGameDto>> GetProfileGamesAsync(string username, int page, int pageSize)
    {
        var query = _uow.Repository<UserGame>().AsQueryable()
            .Where(g => g.User.Username.ToLower() == username.ToLower())
            .OrderByDescending(g => g.AcquiredAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(g => new ProfileGameDto
            {
                Id = g.GameId,
                Title = g.Title,
                ImageUrl = g.ImageUrl,
                Price = g.Price
            }).ToListAsync();

        return new PagedResultDto<ProfileGameDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfilePostDto>> GetProfilePostsAsync(string username, int page, int pageSize)
    {
        var query = _uow.Repository<UserPost>().AsQueryable()
            .Include(p => p.User)
            .Where(p => p.User.Username.ToLower() == username.ToLower())
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProfilePostDto
            {
                Id = p.Id.ToString(),
                Title = p.Title,
                Text = p.Text,
                ImageUrl = p.ImageUrl,
                AuthorUsername = p.User.Username,
                AuthorAvatarUrl = p.User.AvatarUrl,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd"),
                LikesCount = p.LikesCount,
                CommentsCount = p.CommentsCount
            }).ToListAsync();

        return new PagedResultDto<ProfilePostDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileScreenshotDto>> GetProfileScreenshotsAsync(string username, int page, int pageSize)
    {
        var query = _uow.Repository<UserScreenshot>().AsQueryable()
            .Where(s => s.User.Username.ToLower() == username.ToLower())
            .OrderByDescending(s => s.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new ProfileScreenshotDto
            {
                Id = s.Id.ToString(),
                ImageUrl = s.ImageUrl,
                GameTitle = s.GameTitle,
                GameId = s.GameId,
                CreatedAt = s.CreatedAt.ToString("yyyy-MM-dd")
            }).ToListAsync();

        return new PagedResultDto<ProfileScreenshotDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResultDto<ProfileVideoDto>> GetProfileVideosAsync(string username, int page, int pageSize)
    {
        var query = _uow.Repository<UserVideo>().AsQueryable()
            .Where(v => v.User.Username.ToLower() == username.ToLower())
            .OrderByDescending(v => v.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new ProfileVideoDto
            {
                Id = v.Id.ToString(),
                VideoUrl = v.VideoUrl,
                ThumbnailUrl = v.ThumbnailUrl,
                Title = v.Title,
                GameTitle = v.GameTitle,
                GameId = v.GameId,
                CreatedAt = v.CreatedAt.ToString("yyyy-MM-dd")
            }).ToListAsync();

        return new PagedResultDto<ProfileVideoDto>(items, totalCount, page, pageSize);
    }
}
