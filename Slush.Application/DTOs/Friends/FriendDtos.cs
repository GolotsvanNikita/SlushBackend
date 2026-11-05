namespace Slush.Application.DTOs.Friends;

public class FriendUserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int Level { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsOnline { get; set; }

    public string? CurrentGameId { get; set; }
    public string? CurrentGameTitle { get; set; }
}

public class FriendStatusDto
{
    public string Status { get; set; } = string.Empty;
    public Guid? RequestId { get; set; }
}

public class UserSearchResultDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string FriendStatus { get; set; } = string.Empty;
}

public class FriendPlayingDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class FriendRequestDto
{
    public Guid Id { get; set; }
    public FriendUserDto User { get; set; } = null!;
    public string CreatedAt { get; set; } = string.Empty;
}