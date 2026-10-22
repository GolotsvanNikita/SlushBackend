namespace Slush.Domain.Entities;

public class UserReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public Guid ReportedUserId { get; set; }
    public User ReportedUser { get; set; } = null!;

    public Guid? MessageId { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }

    public bool IsResolved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}