var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on localhost:5002
builder.WebHost.UseUrls("http://localhost:5002");

var app = builder.Build();

// In-memory order database
var orders = new List<Order>
{
    new("ORD-001", "CUST-001", 2500m, "Pending", DateTime.Now.AddDays(-2), null),
    new("ORD-002", "CUST-002", 1200m, "Completed", DateTime.Now.AddDays(-5), DateTime.Now.AddDays(-4)),
    new("ORD-003", "CUST-001", 3500m, "Completed", DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-8)),
    new("ORD-004", "CUST-003", 15000m, "Pending", DateTime.Now.AddDays(-1), null),
    new("ORD-005", "CUST-004", 800m, "Failed", DateTime.Now.AddDays(-3), null),
    new("ORD-006", "CUST-002", 950m, "Completed", DateTime.Now.AddDays(-7), DateTime.Now.AddDays(-6)),
    new("ORD-007", "CUST-005", 4200m, "Pending", DateTime.Now.AddHours(-6), null),
    new("ORD-008", "CUST-001", 1800m, "Completed", DateTime.Now.AddDays(-15), DateTime.Now.AddDays(-14)),
    new("ORD-009", "CUST-006", 2100m, "Processing", DateTime.Now.AddHours(-12), null),
    new("ORD-010", "CUST-007", 650m, "Pending", DateTime.Now.AddHours(-3), null),
    new("ORD-011", "CUST-INVALID", 5000m, "Pending", DateTime.Now.AddHours(-1), null), // Customer doesn't exist!
    new("ORD-012", "CUST-003", 22000m, "Processing", DateTime.Now.AddDays(-1), null),
    new("ORD-013", "CUST-008", 3300m, "Completed", DateTime.Now.AddDays(-4), DateTime.Now.AddDays(-3)),
    new("ORD-014", "CUST-009", 28000m, "Pending", DateTime.Now.AddHours(-8), null),
    new("ORD-015", "CUST-004", 1200m, "Failed", DateTime.Now.AddDays(-2), null)
};

var random = new Random();
var requestCount = 0;
var rateLimitCounter = 0;
var rateLimitResetTime = DateTime.Now;

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "OrderApi is running", port = 5002, timestamp = DateTime.Now }));

// GET /api/orders - Get all orders (with filtering)
app.MapGet("/api/orders", async (string? status = null, int page = 1, int pageSize = 10, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] GET /api/orders?status={status}&page={page}&pageSize={pageSize}");
    
    if (!CheckRateLimit())
        return Results.StatusCode(429); // Too Many Requests
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "ERP database connection timeout");
    
    var filteredOrders = string.IsNullOrEmpty(status) 
        ? orders 
        : orders.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
    
    var skip = (page - 1) * pageSize;
    var pagedOrders = filteredOrders.Skip(skip).Take(pageSize).ToList();
    
    Console.WriteLine($"✅ Returning {pagedOrders.Count} orders");
    
    return Results.Ok(new PagedResult<Order>
    {
        Items = pagedOrders,
        Page = page,
        PageSize = pageSize,
        TotalCount = filteredOrders.Count,
        HasNextPage = skip + pageSize < filteredOrders.Count
    });
});

// GET /api/orders/{id} - Get order by ID
app.MapGet("/api/orders/{id}", async (string id, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] GET /api/orders/{id}");
    
    if (!CheckRateLimit())
        return Results.StatusCode(429);
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 503, title: "Order service temporarily unavailable");
    
    var order = orders.FirstOrDefault(o => o.OrderId == id);
    
    if (order == null)
    {
        Console.WriteLine($"❌ Order {id} not found");
        return Results.NotFound(new { error = "Order not found" });
    }
    
    Console.WriteLine($"✅ Returning order: {order.OrderId} (€{order.TotalAmount:N2})");
    return Results.Ok(order);
});

// GET /api/orders/customer/{customerId} - Get orders by customer
app.MapGet("/api/orders/customer/{customerId}", async (string customerId, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] GET /api/orders/customer/{customerId}");
    
    if (!CheckRateLimit())
        return Results.StatusCode(429);
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "Failed to retrieve customer orders");
    
    var customerOrders = orders.Where(o => o.CustomerId == customerId).ToList();
    
    Console.WriteLine($"✅ Found {customerOrders.Count} orders for {customerId}");
    
    return Results.Ok(customerOrders);
});

// POST /api/orders - Create new order
app.MapPost("/api/orders", async (CreateOrderRequest request, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] POST /api/orders");
    
    if (!CheckRateLimit())
        return Results.StatusCode(429);
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "Failed to create order");
    
    // Validate request
    if (string.IsNullOrEmpty(request.CustomerId))
        return Results.BadRequest(new { error = "CustomerId is required" });
    
    if (request.TotalAmount <= 0)
        return Results.BadRequest(new { error = "TotalAmount must be positive" });
    
    var orderId = $"ORD-{orders.Count + 1:D3}";
    var newOrder = new Order(
        orderId,
        request.CustomerId,
        request.TotalAmount,
        "Pending",
        DateTime.Now,
        null
    );
    
    orders.Add(newOrder);
    
    Console.WriteLine($"✅ Created order: {orderId} for {request.CustomerId} (€{request.TotalAmount:N2})");
    
    return Results.Created($"/api/orders/{orderId}", newOrder);
});

// PUT /api/orders/{id}/status - Update order status
app.MapPut("/api/orders/{id}/status", async (string id, UpdateStatusRequest request, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] PUT /api/orders/{id}/status");
    
    if (!CheckRateLimit())
        return Results.StatusCode(429);
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "Failed to update order status");
    
    var order = orders.FirstOrDefault(o => o.OrderId == id);
    
    if (order == null)
    {
        Console.WriteLine($"❌ Order {id} not found");
        return Results.NotFound(new { error = "Order not found" });
    }
    
    var oldStatus = order.Status;
    order.Status = request.NewStatus;
    
    if (request.NewStatus == "Completed")
        order.CompletedAt = DateTime.Now;
    
    Console.WriteLine($"✅ Updated order {id}: {oldStatus} → {request.NewStatus}");
    
    return Results.Ok(new
    {
        orderId = id,
        oldStatus,
        newStatus = request.NewStatus,
        updated = DateTime.Now
    });
});

// POST /api/orders/validate - Validate order before creation
app.MapPost("/api/orders/validate", async (ValidateOrderRequest request, CancellationToken ct = default) =>
{
    requestCount++;
    Console.WriteLine($"[Request #{requestCount}] POST /api/orders/validate");
    
    if (!CheckRateLimit())
        return Results.StatusCode(429);
    
    await SimulateDelay(ct);
    
    if (SimulateError())
        return Results.Problem(statusCode: 500, title: "Validation service error");
    
    var validationErrors = new List<string>();
    
    if (string.IsNullOrEmpty(request.CustomerId))
        validationErrors.Add("CustomerId is required");
    
    if (request.TotalAmount <= 0)
        validationErrors.Add("TotalAmount must be positive");
    
    if (request.TotalAmount > 100000)
        validationErrors.Add("TotalAmount exceeds maximum allowed (€100,000)");
    
    var isValid = !validationErrors.Any();
    
    var result = new
    {
        valid = isValid,
        customerId = request.CustomerId,
        totalAmount = request.TotalAmount,
        errors = validationErrors
    };
    
    Console.WriteLine($"{(isValid ? "✅" : "❌")} Validation: {(isValid ? "Passed" : string.Join(", ", validationErrors))}");
    
    return Results.Ok(result);
});

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║              OrderApi - ERP System                           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("🚀 API is running on: http://localhost:5002");
Console.WriteLine();
Console.WriteLine("Available endpoints:");
Console.WriteLine("  GET  /health                           - Health check");
Console.WriteLine("  GET  /api/orders?status=Pending        - Get all orders (filtered)");
Console.WriteLine("  GET  /api/orders/{id}                  - Get order by ID");
Console.WriteLine("  GET  /api/orders/customer/{customerId} - Get orders by customer");
Console.WriteLine("  POST /api/orders                       - Create new order");
Console.WriteLine("  PUT  /api/orders/{id}/status           - Update order status");
Console.WriteLine("  POST /api/orders/validate              - Validate order data");
Console.WriteLine();
Console.WriteLine("📊 Simulation settings:");
Console.WriteLine("  • Response time: 300-1000ms");
Console.WriteLine("  • Error rate: ~15% (500/503 errors)");
Console.WriteLine("  • Rate limit: 10 requests per 5 seconds");
Console.WriteLine("  • Total orders: 15");
Console.WriteLine();
Console.WriteLine("⚠️  Note: ORD-011 has invalid customer ID for testing!");
Console.WriteLine();
Console.WriteLine("════════════════════════════════════════════════════════════════");

app.Run();

// Helper methods
async Task SimulateDelay(CancellationToken ct)
{
    var delay = random.Next(300, 1000);
    await Task.Delay(delay, ct);
}

bool SimulateError()
{
    // 15% error rate
    return random.Next(100) < 15;
}

bool CheckRateLimit()
{
    // Reset counter every 5 seconds (simulates rate limit window)
    if (DateTime.Now > rateLimitResetTime.AddSeconds(5))
    {
        rateLimitCounter = 0;
        rateLimitResetTime = DateTime.Now;
    }
    
    rateLimitCounter++;
    
    // Max 10 requests per 5 seconds
    if (rateLimitCounter > 10)
    {
        Console.WriteLine("❌ Rate limit exceeded (429)");
        return false;
    }
    
    return true;
}

// Models
public record Order(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt)
{
    public string OrderId { get; set; } = OrderId;
    public string CustomerId { get; set; } = CustomerId;
    public decimal TotalAmount { get; set; } = TotalAmount;
    public string Status { get; set; } = Status; // Pending, Processing, Completed, Failed
    public DateTime CreatedAt { get; set; } = CreatedAt;
    public DateTime? CompletedAt { get; set; } = CompletedAt;
}

public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasNextPage { get; init; }
}

public record CreateOrderRequest(string CustomerId, decimal TotalAmount);

public record UpdateStatusRequest(string NewStatus);

public record ValidateOrderRequest(string CustomerId, decimal TotalAmount);
