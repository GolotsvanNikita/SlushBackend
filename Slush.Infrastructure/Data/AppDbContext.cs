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

        modelBuilder.Entity<Badge>().HasData(
            new Badge
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Золотий зубарик",
                Description = "10 років на сайті",
                XpReward = 500,
                ImageUrl = "https://res.cloudinary.com/asabjy7s/image/upload/v1793918106/Property_1_golden-zubarik.svg"
            },
            new Badge
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Title = "Критик",
                Description = "Опубліковано 25 рецензій",
                XpReward = 120,
                ImageUrl = "https://res.cloudinary.com/asabjy7s/image/upload/v1793918368/Property_1_critic.svg"
            },
            new Badge
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Соціальна пташка",
                Description = "Опубліковано 20 постів кожного типу у Спільноті",
                XpReward = 100,
                ImageUrl = "https://res.cloudinary.com/asabjy7s/image/upload/v1793918355/Property_1_socialbird.svg"
            },
            new Badge
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Title = "Справжній геймер",
                Description = "Куплено 50 ігор",
                XpReward = 220,
                ImageUrl = "https://res.cloudinary.com/asabjy7s/image/upload/v1793918330/Property_1_gamer.svg"
            },
            new Badge
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Title = "Срібний зубарик",
                Description = "5 років на сайті",
                XpReward = 300,
                ImageUrl = "https://res.cloudinary.com/asabjy7s/image/upload/v1793918291/Property_1_silver-zubarik.svg"
            },
            new Badge
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Title = "Картограй",
                Description = "Додано до бібліотеки 10 ігор у жанрі \"карткова гра\"",
                XpReward = 80,
                ImageUrl = "https://res.cloudinary.com/asabjy7s/image/upload/v1793918258/Property_1_cardplayer.svg"
            },
            new Badge
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Title = "Бронзовий зубарик",
                Description = "1 рік на сайті",
                XpReward = 150,
                ImageUrl = "https://res.cloudinary.com/asabjy7s/image/upload/v1793918307/Property_1_bronze-zubarik.svg"
            },
            new Badge
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Title = "Воїн світла",
                Description = "Написано 15 гайдів",
                XpReward = 150,
                ImageUrl = "https://res.cloudinary.com/asabjy7s/image/upload/v1793918320/Property_1_warrior.svg"
            }
        );
    }
}