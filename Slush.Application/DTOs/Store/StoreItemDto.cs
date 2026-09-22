namespace Slush.Application.DTOs.Store
{
    public class StoreItemDto
    {
        public string GameId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public int DiscountPercent { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class AddToStoreRequestDto
    {
        public string GameId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public int DiscountPercent { get; set; }
    }
}