namespace IntegrationService.Models;

public record Customer
{
    public string CustomerId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string LoyaltyTier { get; init; } = string.Empty;
    public decimal CurrentBalance { get; init; }
    public decimal CreditLimit { get; init; }
    public int LoyaltyPoints { get; init; }
}

public record CreditCheckResult
{
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public decimal OrderAmount { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal CreditLimit { get; init; }
    public decimal AvailableCredit { get; init; }
    public bool Approved { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasNextPage { get; init; }
}
