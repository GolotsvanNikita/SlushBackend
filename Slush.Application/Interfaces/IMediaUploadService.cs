using Microsoft.AspNetCore.Http;

namespace Slush.Application.Interfaces;

public interface IMediaUploadService
{
    Task<string?> UploadAvatarAsync(IFormFile file);
    Task<string?> UploadBannerAsync(IFormFile file);
    Task<string?> UploadVideoAsync(IFormFile file);
}