using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Profile
{
    public class NotificationSettingsDto
    {
        public bool BigSale { get; set; }
        public bool WishlistDiscount { get; set; }
        public bool ProfileComment { get; set; }
        public bool FriendRequest { get; set; }
        public bool FriendRequestAccepted { get; set; }
        public bool FriendRequestRejected { get; set; }
        public bool ChatMessageNotification { get; set; }
        public bool ChatMessageSound { get; set; }
    }
}
