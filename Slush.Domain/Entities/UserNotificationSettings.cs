using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Domain.Entities
{
    public class UserNotificationSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public bool BigSale { get; set; } = true;
        public bool WishlistDiscount { get; set; } = true;
        public bool ProfileComment { get; set; } = true;
        public bool FriendRequest { get; set; } = true;
        public bool FriendRequestAccepted { get; set; } = true;
        public bool FriendRequestRejected { get; set; } = true;
        public bool ChatMessageNotification { get; set; } = true;
        public bool ChatMessageSound { get; set; } = true;
    }
}
