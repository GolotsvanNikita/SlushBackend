using System;

namespace Slush.Domain.Entities;

public class Badge
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int XpReward { get; set; }
}