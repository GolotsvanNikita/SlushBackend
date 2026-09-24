namespace Slush.Application.DTOs.Community;

public class CommunityTabCountsDto
{
    public int All { get; set; }
    public int Discussions { get; set; }
    public int Screenshots { get; set; }
    public int Videos { get; set; }
    public int Guides { get; set; }
    public int News { get; set; }
    public bool IsSubscribed { get; set; }
    public int SubscribersCount { get; set; }
}