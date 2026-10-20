using Microsoft.AspNetCore.Http;

namespace Slush.Application.Interfaces;

public interface IMediaUploadService
{
    Task<string?> UploadAvatarAsync(IFormFile file);
    Task<string?> UploadBannerAsync(IFormFile file);
    Task<string?> UploadVideoAsync(IFormFile file);
    Task<string?> UploadScreenshotAsync(IFormFile file);
    Task<string?> UploadChatFileAsync(IFormFile file);
    Task<(string? Url, int? DurationSeconds)> UploadVoiceMessageAsync(IFormFile file);
}