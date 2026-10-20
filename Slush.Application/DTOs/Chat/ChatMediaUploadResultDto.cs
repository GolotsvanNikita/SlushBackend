namespace Slush.Application.DTOs.Chat;

public class ChatMediaUploadResultDto
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FormattedFileSize { get; set; } = string.Empty;
    public int? DurationSeconds { get; set; }
}