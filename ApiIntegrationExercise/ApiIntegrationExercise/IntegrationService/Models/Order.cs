namespace IntegrationService.Models;

public record Order
{
    public string OrderId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record CreateOrderRequest
{
    public string CustomerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}

public record UpdateStatusRequest
{
    public string NewStatus { get; init; } = string.Empty;
}
