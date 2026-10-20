using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Npgsql.BackendMessages;
using Slush.Application.Interfaces;

namespace Slush.Infrastructure.Services;

public class MediaUploadService : IMediaUploadService
{
    private readonly Cloudinary _cloudinary;

    public MediaUploadService(IOptions<CloudinarySettings> config)
    {
        var acc = new Account(
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecret
        );

        _cloudinary = new Cloudinary(acc);
    }

    public async Task<string?> UploadAvatarAsync(IFormFile file)
    {
        if (file.Length == 0) return null;

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face"),
            Folder = "slush/avatars"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        return uploadResult.SecureUrl?.ToString();
    }

    public async Task<string?> UploadBannerAsync(IFormFile file)
    {
        if (file.Length == 0) return null;

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Transformation = new Transformation().Height(400).Width(1200).Crop("fill"),
            Folder = "slush/banners"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        return uploadResult.SecureUrl?.ToString();
    }

    public async Task<string?> UploadVideoAsync(IFormFile file)
    {
        if (file.Length == 0) return null;

        await using var stream = file.OpenReadStream();

        var uploadParams = new VideoUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "slush/videos"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        return uploadResult.SecureUrl?.ToString();
    }

    public async Task<string?> UploadScreenshotAsync(IFormFile file)
    {
        if (file.Length == 0) return null;

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Transformation = new Transformation().Width(1920).Height(1080).Crop("limit"),
            Folder = "slush/screenshots"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        return uploadResult.SecureUrl?.ToString();
    }

    public async Task<string?> UploadChatFileAsync(IFormFile file)
    {
        if (file.Length == 0) return null;

        await using var stream = file.OpenReadStream();
        var uploadParams = new CloudinaryDotNet.Actions.RawUploadParams
        {
            File = new CloudinaryDotNet.FileDescription(file.FileName, stream),
            Folder = "slush/chat_files"
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        return result.Error != null ? null : result.SecureUrl.ToString();
    }

    public async Task<(string? Url, int? DurationSeconds)> UploadVoiceMessageAsync(IFormFile file)
    {
        if (file.Length == 0) return (null, null);

        await using var stream = file.OpenReadStream();
        var uploadParams = new CloudinaryDotNet.Actions.VideoUploadParams
        {
            File = new CloudinaryDotNet.FileDescription(file.FileName, stream),
            Folder = "slush/chat_voices"
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null) return (null, null);

        int? duration = result.Duration > 0 ? (int)Math.Round(result.Duration) : null;
        return (result.SecureUrl.ToString(), duration);
    }
}