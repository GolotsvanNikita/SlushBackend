using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Profile;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _uow;

    public SettingsService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ProfileSettingsDto> GetProfileSettingsAsync(Guid userId)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        return new ProfileSettingsDto
        {
            Username = user.Username,
            Email = user.Email,
            Bio = user.Bio,
            Language = user.Language,
            AvatarUrl = user.AvatarUrl,
            CoverUrl = user.CoverUrl
        };
    }

    public async Task UpdateProfileSettingsAsync(Guid userId, UpdateProfileSettingsDto request)
    {
        var repo = _uow.Repository<User>();
        var user = await repo.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        if (user.Username != request.Username && await repo.AsQueryable().AnyAsync(u => u.Username == request.Username))
            throw new Exception("Username is already taken.");

        if (user.Email != request.Email && await repo.AsQueryable().AnyAsync(u => u.Email == request.Email))
            throw new Exception("Email is already taken.");

        user.Username = request.Username;
        user.Email = request.Email;
        user.Bio = request.Bio;
        user.Language = request.Language;

        repo.Update(user);
        await _uow.SaveChangesAsync();
    }

    public async Task<NotificationSettingsDto> GetNotificationSettingsAsync(Guid userId)
    {
        var repo = _uow.Repository<UserNotificationSettings>();
        var settings = await repo.AsQueryable().FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            settings = new UserNotificationSettings { UserId = userId };
            await repo.AddAsync(settings);
            await _uow.SaveChangesAsync();
        }

        return new NotificationSettingsDto
        {
            BigSale = settings.BigSale,
            WishlistDiscount = settings.WishlistDiscount,
            ProfileComment = settings.ProfileComment,
            FriendRequest = settings.FriendRequest,
            FriendRequestAccepted = settings.FriendRequestAccepted,
            FriendRequestRejected = settings.FriendRequestRejected,
            ChatMessageNotification = settings.ChatMessageNotification,
            ChatMessageSound = settings.ChatMessageSound
        };
    }

    public async Task UpdateNotificationSettingsAsync(Guid userId, NotificationSettingsDto request)
    {
        var repo = _uow.Repository<UserNotificationSettings>();
        var settings = await repo.AsQueryable().FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            settings = new UserNotificationSettings { UserId = userId };
            await repo.AddAsync(settings);
        }

        settings.BigSale = request.BigSale;
        settings.WishlistDiscount = request.WishlistDiscount;
        settings.ProfileComment = request.ProfileComment;
        settings.FriendRequest = request.FriendRequest;
        settings.FriendRequestAccepted = request.FriendRequestAccepted;
        settings.FriendRequestRejected = request.FriendRequestRejected;
        settings.ChatMessageNotification = request.ChatMessageNotification;
        settings.ChatMessageSound = request.ChatMessageSound;

        repo.Update(settings);
        await _uow.SaveChangesAsync();
    }
}