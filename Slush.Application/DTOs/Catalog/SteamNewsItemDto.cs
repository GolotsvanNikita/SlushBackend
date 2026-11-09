namespace Slush.Application.DTOs.Catalog;

public class SteamNewsItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Contents { get; set; } = string.Empty;
    public string FeedLabel { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string DateFormatted { get; set; } = string.Empty;
}