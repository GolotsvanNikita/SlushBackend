using Microsoft.EntityFrameworkCore;
using Slush.Domain.Entities;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}