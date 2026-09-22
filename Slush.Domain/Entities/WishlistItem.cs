namespace Slush.Domain.Entities
{
    public class WishlistItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string GameId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public int DiscountPercent { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}