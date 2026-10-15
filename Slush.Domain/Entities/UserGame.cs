namespace Slush.Domain.Entities;

public class UserGame
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public string? HeaderImage { get; set; }
    public string? Developer { get; set; }
    public string? ReleaseDate { get; set; }
    public string? DiskSize { get; set; }
    public bool? IsInstalled { get; set; }

    public bool IsFavorite { get; set; } = false;

    public Guid? CollectionId { get; set; }
    public LibraryCollection? Collection { get; set; }

    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}