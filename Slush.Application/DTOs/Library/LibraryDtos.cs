namespace Slush.Application.DTOs.Library;

public class LibraryGameDto
{
    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    public string? HeaderImage { get; set; }
    public string? Developer { get; set; }
    public string? ReleaseDate { get; set; }
    public string? DiskSize { get; set; }
    public bool IsFavorite { get; set; }
    public Guid? CollectionId { get; set; }
    public bool? IsInstalled { get; set; }
    public string AcquiredAt { get; set; } = string.Empty;
}

public class LibraryCollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GamesCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class SaveCollectionRequestDto
{
    public string Name { get; set; } = string.Empty;
}