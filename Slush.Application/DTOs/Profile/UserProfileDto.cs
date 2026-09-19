namespace Slush.Application.DTOs.Profile;

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;

    public int Level { get; set; }
    public int CurrentXp { get; set; }
    public int MaxXp { get; set; }

    public ProfileCountersDto Counters { get; set; } = new();

    public List<BadgeDto> Badges { get; set; } = new();

    public List<FriendDto> Friends { get; set; } = new();
}

public class ProfileCountersDto
{
    public int Badges { get; set; }
    public int Games { get; set; }
    public int Wishlist { get; set; }
    public int Discussions { get; set; }
    public int Screenshots { get; set; }
    public int Videos { get; set; }
    public int Guides { get; set; }
    public int Reviews { get; set; }
    public int Friends { get; set; }
}

public class BadgeDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string EarnedAt { get; set; } = string.Empty;
}

public class FriendDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int Level { get; set; }
}