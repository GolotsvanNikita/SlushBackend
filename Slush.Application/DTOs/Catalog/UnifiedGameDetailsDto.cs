namespace Slush.Application.DTOs.Catalog;

public class UnifiedGameDetailsDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public List<ScreenshotDto> Screenshots { get; set; } = new();
    public List<GameVideoDto> Videos { get; set; } = new();

    public decimal Price { get; set; }
    public decimal OldPrice { get; set; }
    public int DiscountPercent { get; set; }
    public bool IsFree { get; set; } = false;

    public List<string> Tags { get; set; } = new();

    public List<DlcDto> DLCs { get; set; } = new();
    public List<BundleDto> Bundles { get; set; } = new();

    public double AverageRating { get; set; }
    public List<GameReviewDto> Reviews { get; set; } = new();
    public List<FriendPlayingDto> FriendsPlaying { get; set; } = new();

    public bool IsInWishlist { get; set; }
    public bool IsInCart { get; set; }
    public PcRequirementsDto PcRequirements { get; set; } = new();
}

public class GameReviewDto
{
    public string Username { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

public class ScreenshotDto
{
    public int Id { get; set; }
    public string Image { get; set; } = string.Empty;
}

public class GameVideoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string VideoUrlWebm { get; set; } = string.Empty;
    public string VideoUrlMp4 { get; set; } = string.Empty;
}

public class FriendPlayingDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class DlcDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class BundleDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class PcRequirementsDto
{
    public string Minimum { get; set; } = string.Empty;
    public string Recommended { get; set; } = string.Empty;
}