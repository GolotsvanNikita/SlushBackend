using Microsoft.EntityFrameworkCore;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Domain.Enums;

namespace Slush.Infrastructure.Services;

public class BadgeService : IBadgeService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public BadgeService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    private async Task AwardBadgeAsync(User user, string badgeTitle)
    {
        var badge = await _uow.Repository<Badge>().AsQueryable()
            .FirstOrDefaultAsync(b => b.Title == badgeTitle);

        if (badge == null) return;

        bool hasBadge = await _uow.Repository<UserBadge>().AsQueryable()
            .AnyAsync(ub => ub.UserId == user.Id && ub.BadgeId == badge.Id);

        if (!hasBadge)
        {
            await _uow.Repository<UserBadge>().AddAsync(new UserBadge
            {
                UserId = user.Id,
                BadgeId = badge.Id,
                EarnedAt = DateTime.UtcNow
            });

            user.CurrentXp += badge.XpReward;

            int maxXpForCurrentLevel = (user.Level == 0 ? 1 : user.Level) * 1000 + 500;
            if (user.CurrentXp >= maxXpForCurrentLevel)
            {
                user.Level += 1;
                user.CurrentXp -= maxXpForCurrentLevel;
            }

            _uow.Repository<User>().Update(user);
            await _uow.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                user.Id,
                $"Ви здобули новий значок: «{badge.Title}»!",
                "SystemAlert");
        }
    }

    public async Task CheckReviewBadgesAsync(Guid userId)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user == null) return;

        var reviewsCount = await _uow.Repository<GameReview>().AsQueryable()
            .CountAsync(r => r.UserId == userId);

        if (reviewsCount >= 25)
        {
            await AwardBadgeAsync(user, "Критик");
        }
    }

    public async Task CheckCommunityBadgesAsync(Guid userId)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user == null) return;

        var postsQuery = _uow.Repository<CommunityPost>().AsQueryable()
            .Where(p => p.UserId == userId);

        var guidesCount = await postsQuery.CountAsync(p => p.PostType == CommunityPostType.Guide);
        if (guidesCount >= 15)
        {
            await AwardBadgeAsync(user, "Воїн світла");
        }

        var discussionsCount = await postsQuery.CountAsync(p => p.PostType == CommunityPostType.Discussion);
        var screenshotsCount = await postsQuery.CountAsync(p => p.PostType == CommunityPostType.Screenshot);
        var videosCount = await postsQuery.CountAsync(p => p.PostType == CommunityPostType.Video);

        if (guidesCount >= 20 && discussionsCount >= 20 && screenshotsCount >= 20 && videosCount >= 20)
        {
            await AwardBadgeAsync(user, "Соціальна пташка");
        }
    }

    public async Task CheckGamePurchasesBadgesAsync(Guid userId)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user == null) return;

        var gamesCount = await _uow.Repository<UserGame>().AsQueryable()
            .CountAsync(g => g.UserId == userId);

        if (gamesCount >= 50)
        {
            await AwardBadgeAsync(user, "Справжній геймер");
        }
    }

    public async Task CheckAccountAgeBadgesAsync()
    {
        var now = DateTime.UtcNow;
        var oneYearAgo = now.AddYears(-1);
        var fiveYearsAgo = now.AddYears(-5);
        var tenYearsAgo = now.AddYears(-10);

        var eligibleUsers = await _uow.Repository<User>().AsQueryable()
            .Include(u => u.UserBadges)
            .Where(u => u.CreatedAt <= oneYearAgo)
            .ToListAsync();

        foreach (var user in eligibleUsers)
        {
            if (user.CreatedAt <= tenYearsAgo)
                await AwardBadgeAsync(user, "Золотий зубарик");
            else if (user.CreatedAt <= fiveYearsAgo)
                await AwardBadgeAsync(user, "Срібний зубарик");
            else
                await AwardBadgeAsync(user, "Бронзовий зубарик");
        }
    }
}