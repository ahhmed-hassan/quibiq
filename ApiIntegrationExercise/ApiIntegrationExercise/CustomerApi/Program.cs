using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on localhost:5001
builder.WebHost.UseUrls("http://localhost:5001");

var app = builder.Build();

// In-memory customer database
var customers = new List<Customer>
{
    new("CUST-001", "Ahmed Hassan", "ahmed@example.com", 15000m, 25000m, 450),
    new("CUST-002", "Sarah Miller", "sarah@example.com", 8000m, 15000m, 280),
    new("CUST-003", "James Wilson", "james@example.com", 50000m, 100000m, 1200),
    new("CUST-004", "Maria Garcia", "maria@example.com", 2000m, 5000m, 80),
    new("CUST-005", "David Chen", "david@example.com",  12000m, 20000m, 390),
    new("CUST-006", "Emma Johnson", "emma@example.com", 6500m, 12000m, 210),
    new("CUST-007", "Michael Brown", "michael@example.com", 1800m, 5000m, 65),
    new("CUST-008", "Lisa Anderson", "lisa@example.com",  18000m, 30000m, 520),
    new("CUST-009", "Robert Taylor", "robert@example.com",  75000m, 150000m, 1850),
    new("CUST-010", "Jennifer Lee", "jennifer@example.com",  9500m, 18000m, 310)
};

var random = new Random();
var requestCount = 0;

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "CustomerApi is running", port = 5001, timestamp = DateTime.Now }));

// GET /api/customers - Get all customers (paginated)
app.MapGet("/api/customers", async (int page = 1, int pageSize = 10, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] GET /api/customers?page={page}&pageSize={pageSize}");
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "Database connection error");
    
    var skip = (page - 1) * pageSize;
    var pagedCustomers = customers.Skip(skip).Take(pageSize).ToList();
    
    Console.WriteLine($"✅ Returning {pagedCustomers.Count} customers");
    
    return Results.Ok(new PagedResult<Customer>
    {
        Items = pagedCustomers,
        Page = page,
        PageSize = pageSize,
        TotalCount = customers.Count,
        HasNextPage = skip + pageSize < customers.Count
    });
});

// GET /api/customers/{id} - Get customer by ID
app.MapGet("/api/customers/{id}", async (string id, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] GET /api/customers/{id}");
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "CRM system temporarily unavailable");
    
    var customer = customers.FirstOrDefault(c => c.CustomerId == id);
    
    if (customer == null)
    {
        Console.WriteLine($"❌ Customer {id} not found");
        return Results.NotFound(new { error = "Customer not found" });
    }
    
    Console.WriteLine($"✅ Returning customer: {customer.Name}");
    return Results.Ok(customer);
});

// GET /api/customers/{id}/credit-check - Check if customer can place order
app.MapGet("/api/customers/{id}/credit-check", async (string id, decimal orderAmount, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] GET /api/customers/{id}/credit-check?orderAmount={orderAmount:C}");
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 503, title: "Credit check service unavailable");
    
    var customer = customers.FirstOrDefault(c => c.CustomerId == id);
    
    if (customer == null)
    {
        Console.WriteLine($"❌ Customer {id} not found");
        return Results.NotFound(new { error = "Customer not found" });
    }
    
    var availableCredit = customer.CreditLimit - customer.CurrentBalance;
    var approved = availableCredit >= orderAmount;
    
    var result = new CreditCheckResult
    {
        CustomerId = id,
        CustomerName = customer.Name,
        OrderAmount = orderAmount,
        CurrentBalance = customer.CurrentBalance,
        CreditLimit = customer.CreditLimit,
        AvailableCredit = availableCredit,
        Approved = approved,
        Reason = approved ? "Credit approved" : "Insufficient credit limit"
    };
    
    Console.WriteLine($"{(approved ? "✅" : "❌")} Credit check: {result.Reason}");
    return Results.Ok(result);
});

// PUT /api/customers/{id}/loyalty - Update customer loyalty points and tier
app.MapPut("/api/customers/{id}/loyalty", async (string id, LoyaltyUpdate update, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] PUT /api/customers/{id}/loyalty");
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "Failed to update loyalty data");
    
    var customer = customers.FirstOrDefault(c => c.CustomerId == id);
    
    if (customer == null)
    {
        Console.WriteLine($"❌ Customer {id} not found");
        return Results.NotFound(new { error = "Customer not found" });
    }
    
    // Update loyalty points
    customer.LoyaltyPoints += update.PointsToAdd;
    
    // Update tier based on points
    var oldTier = customer.LoyaltyTier;
    //customer.LoyaltyTier = CalculateTier(customer.LoyaltyPoints);
    
    Console.WriteLine($"✅ Updated {customer.Name}: {oldTier} → {customer.LoyaltyTier} ({customer.LoyaltyPoints} points)");
    
    return Results.Ok(new
    {
        customerId = id,
        oldTier,
        newTier = customer.LoyaltyTier,
        totalPoints = customer.LoyaltyPoints,
        updated = DateTime.Now
    });
});

// POST /api/customers/{id}/balance - Update customer balance
app.MapPost("/api/customers/{id}/balance", async (string id, BalanceUpdate update, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] POST /api/customers/{id}/balance");
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "Failed to update balance");
    
    var customer = customers.FirstOrDefault(c => c.CustomerId == id);
    
    if (customer == null)
    {
        Console.WriteLine($"❌ Customer {id} not found");
        return Results.NotFound(new { error = "Customer not found" });
    }
    
    var oldBalance = customer.CurrentBalance;
    customer.CurrentBalance += update.Amount;
    
    // Ensure balance doesn't go negative
    if (customer.CurrentBalance < 0)
        customer.CurrentBalance = 0;
    
    Console.WriteLine($"✅ Updated balance: {oldBalance:C} → {customer.CurrentBalance:C}");
    
    return Results.Ok(new
    {
        customerId = id,
        oldBalance,
        newBalance = customer.CurrentBalance,
        change = update.Amount,
        updated = DateTime.Now
    });
});

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║              CustomerApi - CRM System                        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("🚀 API is running on: http://localhost:5001");
Console.WriteLine();
Console.WriteLine("Available endpoints:");
Console.WriteLine("  GET  /health                              - Health check");
Console.WriteLine("  GET  /api/customers?page=1&pageSize=10    - Get all customers (paginated)");
Console.WriteLine("  GET  /api/customers/{id}                  - Get customer by ID");
Console.WriteLine("  GET  /api/customers/{id}/credit-check     - Check credit limit");
Console.WriteLine("  PUT  /api/customers/{id}/loyalty          - Update loyalty points");
Console.WriteLine("  POST /api/customers/{id}/balance          - Update balance");
Console.WriteLine();
Console.WriteLine("📊 Simulation settings:");
Console.WriteLine("  • Response time: 200-800ms");
Console.WriteLine("  • Error rate: ~20% (500/503 errors)");
Console.WriteLine("  • Total customers: 10");
Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");

app.Run();

// Helper methods
async Task SimulateDelay(CancellationToken ct)
{
    var delay = random.Next(200, 800);
    await Task.Delay(delay, ct);
}

bool SimulateError()
{
    // 20% error rate
    return random.Next(100) < 20;
}



// Models
public record Customer(
    string CustomerId,
    string Name,
    string Email,
    decimal CurrentBalance,
    decimal CreditLimit,
    int LoyaltyPoints)
{
    public string CustomerId { get; set; } = CustomerId;
    public string Name { get; set; } = Name;
    public string Email { get; set; } = Email;
    public string LoyaltyTier  => CalculateTier(LoyaltyPoints); 
    public decimal CurrentBalance { get; set; } = CurrentBalance;
    public decimal CreditLimit { get; set; } = CreditLimit;
    public int LoyaltyPoints { get; set; } = LoyaltyPoints;
    private string CalculateTier(int points) => points switch
    {
        >= 1000 => "Platinum",
        >= 500 => "Gold",
        >= 200 => "Silver",
        _ => "Bronze"
    };
}

public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasNextPage { get; init; }
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


public record LoyaltyUpdate(int PointsToAdd);

public record BalanceUpdate(decimal Amount);
