using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public UserRole Role { get; set; } = UserRole.User;
        public bool IsBanned { get; set; } = false;

        public string Bio { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public int CurrentXp { get; set; } = 0;

        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
        public ICollection<ProfileComment> ProfileComments { get; set; } = new List<ProfileComment>(); // Комментарии на ЕГО стене
        public ICollection<UserGuide> Guides { get; set; } = new List<UserGuide>();
    }
}
