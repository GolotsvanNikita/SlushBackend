using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Mozilla;
using Slush.Domain.Entities;
using System.Reflection.Emit;

namespace Slush.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<VerificationCode> VerificationCodes { get; set; }
    public DbSet<ActivitySnapshot> ActivitySnapshots { get; set; }
    public DbSet<AlertRule> AlertRules { get; set; }
    public DbSet<AlertHistory> AlertHistories { get; set; }
    public DbSet<NotificationConfig> NotificationConfigs { get; set; }
    public DbSet<UserLoginHistory> UserLoginHistories { get; set; }
    public DbSet<GameStatsSnapshot> GameStatsSnapshots { get; set; }
    public DbSet<AdminActionLog> AdminActionLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<GameReview> GameReviews { get; set; }
    public DbSet<Badge> Badges { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<ProfileComment> ProfileComments { get; set; }
    public DbSet<UserGame> UserGames { get; set; }
    public DbSet<CommunityPost> CommunityPosts { get; set; }
    public DbSet<PostLike> PostLikes { get; set; }
    public DbSet<PostComment> PostComments { get; set; }
    public DbSet<GameSubscription> GameSubscriptions { get; set; }
    public DbSet<UserNotificationSettings> UserNotificationSettings { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<UserPasswordHistory> UserPasswordHistories { get; set; }
    public DbSet<FriendRequest> FriendRequests { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<LibraryCollection> LibraryCollections { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatConversationSetting> ChatConversationSettings { get; set; }
    public DbSet<UserReport> UserReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        modelBuilder.Entity<UserBadge>()
            .HasKey(ub => new { ub.UserId, ub.BadgeId });

        modelBuilder.Entity<ProfileComment>()
            .HasOne(pc => pc.ProfileUser)
            .WithMany(u => u.ProfileComments)
            .HasForeignKey(pc => pc.ProfileUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProfileComment>()
            .HasOne(pc => pc.Author)
            .WithMany()
            .HasForeignKey(pc => pc.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PostLike>()
            .HasOne(pl => pl.User)
            .WithMany()
            .HasForeignKey(pl => pl.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PostComment>()
            .HasOne(pc => pc.User)
            .WithMany()
            .HasForeignKey(pc => pc.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FriendRequest>()
            .HasIndex(x => new { x.SenderId, x.ReceiverId, x.Status });

        modelBuilder.Entity<FriendRequest>()
            .HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FriendRequest>()
            .HasOne(x => x.Receiver)
            .WithMany()
            .HasForeignKey(x => x.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LibraryCollection>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserGame>()
            .HasOne(ug => ug.Collection)
            .WithMany(c => c.Games)
            .HasForeignKey(ug => ug.CollectionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserGame>()
            .HasIndex(ug => new { ug.UserId, ug.GameId })
            .IsUnique();

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => new { m.SenderId, m.ReceiverId, m.CreatedAt });
            entity.HasIndex(m => new { m.ReceiverId, m.IsRead });
        });

        modelBuilder.Entity<ChatConversationSetting>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Friend)
                .WithMany()
                .HasForeignKey(s => s.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new { s.UserId, s.FriendId }).IsUnique();
        });

        modelBuilder.Entity<UserReport>(entity =>
        {
            entity.HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.ReportedUser)
                .WithMany()
                .HasForeignKey(r => r.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => r.CreatedAt);
        });
    }
}