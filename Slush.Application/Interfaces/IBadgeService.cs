namespace Slush.Application.Interfaces;

public interface IBadgeService
{
    Task CheckReviewBadgesAsync(Guid userId);

    Task CheckCommunityBadgesAsync(Guid userId);

    Task CheckGamePurchasesBadgesAsync(Guid userId);

    Task CheckAccountAgeBadgesAsync();
}