namespace Slush.Domain.Entities
{
    public class UserScreenshot
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string GameId { get; set; } = string.Empty;
        public string GameTitle { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
