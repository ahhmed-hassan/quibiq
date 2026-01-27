using System.Net;
using System.Net.Http.Json;
using IntegrationService.Infrastructure;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Services;

/// <summary>
/// OrderService - Consumes OrderApi (ERP System) using Polly resilience pipelines
/// 
/// Key Challenge: OrderApi has rate limiting (10 requests per 5 seconds)
/// Solution: Use CreateRateLimitedHttpPipeline to handle 429 responses
/// </summary>
public class OrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderService> _logger;
    
    public OrderService(HttpClient httpClient, ILogger<OrderService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 1: GetOrdersAsync - Rate-limited pipeline
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get orders with optional filtering
    /// Uses rate-limited pipeline to handle 429 (Too Many Requests) responses
    /// </summary>
    public async Task<PagedResult<Order>?> GetOrdersAsync(
        string? status = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        // Build query string
        var queryParams = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status))
            queryParams += $"&status={status}";
        
        // Create rate-limited pipeline (handles 429 + transient errors)
        var pipeline = ResiliencePipelineFactory.CreateRateLimitedHttpPipeline(
            operationName: "GetOrders",
            logger: _logger,
            endpoint: $"/api/orders{queryParams}",
            maxRetries: 5,
            rateLimitWaitSeconds: 5
        );
        
        // Execute through pipeline
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.GetAsync($"/api/orders{queryParams}", token);
        }, ct);
        
        // Handle errors
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get orders after retries: {StatusCode}",
                response.StatusCode);
            return null;
        }
        
        // Deserialize
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Order>>(ct);
        
        _logger.LogInformation(
            "✓ Retrieved {Count} orders (page {Page})",
            result?.Items.Count ?? 0, page);
        
        return result;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 2: GetOrderAsync - Rate-limited pipeline
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get single order by ID
    /// </summary>
    public async Task<Order?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var pipeline = ResiliencePipelineFactory.CreateRateLimitedHttpPipeline(
            operationName: "GetOrder",
            logger: _logger,
            endpoint: $"/api/orders/{orderId}",
            maxRetries: 5,
            rateLimitWaitSeconds: 5
        );
        
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.GetAsync($"/api/orders/{orderId}", token);
        }, ct);
        
        // Handle 404
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        
        var order = await response.Content.ReadFromJsonAsync<Order>(ct);
        _logger.LogInformation("✓ Retrieved order {OrderId}", orderId);
        
        return order;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 3: GetOrdersByCustomerAsync - Rate-limited pipeline
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get all orders for a specific customer
    /// Useful for calculating customer loyalty points
    /// </summary>
    public async Task<List<Order>> GetOrdersByCustomerAsync(
        string customerId, 
        CancellationToken ct = default)
    {
        var pipeline = ResiliencePipelineFactory.CreateRateLimitedHttpPipeline(
            operationName: "GetOrdersByCustomer",
            logger: _logger,
            endpoint: $"/api/orders/customer/{customerId}",
            maxRetries: 5,
            rateLimitWaitSeconds: 5
        );
        
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.GetAsync($"/api/orders/customer/{customerId}", token);
        }, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Failed to get orders for customer {CustomerId}: {StatusCode}",
                customerId, response.StatusCode);
            return new List<Order>();
        }
        
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>(ct) 
                     ?? new List<Order>();
        
        _logger.LogInformation(
            "✓ Retrieved {Count} orders for customer {CustomerId}",
            orders.Count, customerId);
        
        return orders;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 4: UpdateOrderStatusAsync - Rate-limited pipeline
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Update order status (Pending → Processing → Completed or Failed)
    /// </summary>
    public async Task<bool> UpdateOrderStatusAsync(
        string orderId, 
        string newStatus, 
        CancellationToken ct = default)
    {
        var pipeline = ResiliencePipelineFactory.CreateRateLimitedHttpPipeline(
            operationName: "UpdateOrderStatus",
            logger: _logger,
            endpoint: $"/api/orders/{orderId}/status",
            maxRetries: 5,
            rateLimitWaitSeconds: 5
        );
        
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.PutAsJsonAsync(
                $"/api/orders/{orderId}/status",
                new UpdateStatusRequest{ NewStatus = newStatus },
                token);
        }, ct);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
            return false;
        }
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "✓ Updated order {OrderId} status to {Status}",
                orderId, newStatus);
            return true;
        }
        
        _logger.LogWarning(
            "Failed to update order {OrderId} status: {StatusCode}",
            orderId, response.StatusCode);
        return false;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 5: CreateOrderAsync - Rate-limited pipeline
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Create a new order
    /// </summary>
    public async Task<Order?> CreateOrderAsync(
        string customerId,
        decimal totalAmount,
        CancellationToken ct = default)
    {
        var pipeline = ResiliencePipelineFactory.CreateRateLimitedHttpPipeline(
            operationName: "CreateOrder",
            logger: _logger,
            endpoint: "/api/orders",
            maxRetries: 5,
            rateLimitWaitSeconds: 5
        );
        
        var request = new { CustomerId = customerId, TotalAmount = totalAmount };
        
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.PostAsJsonAsync("/api/orders", request, token);
        }, ct);
        
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(
                "Invalid order data for customer {CustomerId}: {Amount:C}",
                customerId, totalAmount);
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        
        var order = await response.Content.ReadFromJsonAsync<Order>(ct);
        _logger.LogInformation(
            "✓ Created order {OrderId} for customer {CustomerId}: {Amount:C}",
            order?.OrderId, customerId, totalAmount);
        
        return order;
    }
}
