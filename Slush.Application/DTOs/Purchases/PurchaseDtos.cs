namespace Slush.Application.DTOs.Purchases;

public class PurchaseResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public string Currency { get; set; } = "UAH";
    public decimal NewBalance { get; set; }
    public string PurchasedAt { get; set; } = string.Empty;
}

public class CheckoutResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PurchasedCount { get; set; }
    public decimal TotalPaid { get; set; }
    public string Currency { get; set; } = "UAH";
    public decimal NewBalance { get; set; }
    public List<PurchaseResultDto> Items { get; set; } = [];
}