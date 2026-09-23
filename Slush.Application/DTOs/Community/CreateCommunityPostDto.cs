using System.ComponentModel.DataAnnotations;
using Slush.Domain.Enums;

namespace Slush.Application.DTOs.Community;

public class CreateCommunityPostDto
{
    [Required]
    public string GameId { get; set; } = string.Empty;

    [Required]
    public CommunityPostType PostType { get; set; }

    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ShortDescription { get; set; }

    public string? MediaUrl { get; set; }
}