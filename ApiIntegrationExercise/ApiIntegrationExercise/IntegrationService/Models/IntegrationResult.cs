namespace IntegrationService.Models;

public record IntegrationResult
{
    public int TotalOrdersProcessed { get; init; }
    public int SuccessfulOrders { get; init; }
    public int FailedOrders { get; init; }
    public List<string> FailedOrderIds { get; init; } = new();
    public List<string> ErrorMessages { get; init; } = new();
    public TimeSpan Duration { get; init; }
}

public record OrderValidationResult
{
    public string OrderId { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
    public Customer? Customer { get; init; }
    public CreditCheckResult? CreditCheck { get; init; }
}
