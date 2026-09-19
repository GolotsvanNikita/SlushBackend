namespace Slush.Application.DTOs.Catalog;

public class UnifiedGameDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public Domain.Enums.GameSource Source { get; set; }

    public decimal Price { get; set; }
    public decimal OldPrice { get; set; }
    public int DiscountPercent { get; set; }

    public string Thumbnail { get; set; } = string.Empty;
    public string DealUrl { get; set; } = string.Empty;

    public string ReleaseDate { get; set; } = string.Empty;

    public UnifiedGameDto(
        string id,
        string title,
        Domain.Enums.GameSource source,
        decimal price,
        decimal oldPrice,
        int discountPercent,
        string thumbnail,
        string dealUrl,
        string releaseDate)
    {
        Id = id;
        Title = title;
        Source = source;
        Price = price;
        OldPrice = oldPrice;
        DiscountPercent = discountPercent;
        Thumbnail = thumbnail;
        DealUrl = dealUrl;
        ReleaseDate = releaseDate;
    }
}