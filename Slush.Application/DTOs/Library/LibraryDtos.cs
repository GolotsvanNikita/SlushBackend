using Slush.Application.DTOs.Community;
using Slush.Application.DTOs.Friends;

namespace Slush.Application.DTOs.Library;

public class LibraryGameDto
{
    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    public string? HeaderImage { get; set; }
    public string? Developer { get; set; }
    public string? ReleaseDate { get; set; }
    public string? DiskSize { get; set; }
    public bool IsFavorite { get; set; }
    public Guid? CollectionId { get; set; }
    public bool? IsInstalled { get; set; }
    public string AcquiredAt { get; set; } = string.Empty;
}

public class LibraryCollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GamesCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class SaveCollectionRequestDto
{
    public string Name { get; set; } = string.Empty;
}

public class LibraryHomeDto
{
    public List<LibraryGameDto> RecentGames { get; set; } = [];
    public List<LibraryGameDto> FavoriteGames { get; set; } = [];
    public List<LibraryCollectionDto> Collections { get; set; } = [];
    public List<CommunityPostDto> LatestNews { get; set; } = [];
    public List<CommunityPostDto> CommunityHighlights { get; set; } = [];
}

public class GameLibraryContextDto
{
    public string GameId { get; set; } = string.Empty;
    public bool IsOwned { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInWishlist { get; set; }
    public bool IsInCart { get; set; }
    public Guid? CollectionId { get; set; }
    public string? DiskSize { get; set; }
    public bool? IsInstalled { get; set; }

    public UserGameReviewDto? MyReview { get; set; }
    public List<FriendUserDto> FriendsWhoOwn { get; set; } = [];
    public List<FriendUserDto> FriendsWhoWant { get; set; } = [];
}

public class UserGameReviewDto
{
    public Guid Id { get; set; }
    public string GameId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Text { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class SaveGameReviewRequestDto
{
    public string GameTitle { get; set; } = string.Empty;
    public string GameBannerUrl { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Text { get; set; } = string.Empty;
}