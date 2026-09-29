namespace Slush.Application.DTOs.Friends;

public class FriendUserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public class FriendRequestDto
{
    public Guid Id { get; set; }
    public FriendUserDto User { get; set; } = null!;
    public string CreatedAt { get; set; } = string.Empty;
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
    public string FriendStatus { get; set; } = "None";
}