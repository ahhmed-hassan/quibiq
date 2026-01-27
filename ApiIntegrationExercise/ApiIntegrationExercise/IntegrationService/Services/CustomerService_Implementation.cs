using System.Net;
using System.Net.Http.Json;
using IntegrationService.Infrastructure;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace IntegrationService.Services;

/// <summary>
/// CustomerService - Consumes CustomerApi (CRM System) using Polly resilience pipelines
/// </summary>
public class CustomerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerService> _logger;
    
    public CustomerService(HttpClient httpClient, ILogger<CustomerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }


    public async Task<PagedResult<Customer>?> GetCustomersAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        // Build query string
        var queryParams = $"?page={page}&pageSize={pageSize}";

        // Create rate-limited pipeline (handles 429 + transient errors)
        var pipeline = ResiliencePipelineFactory.CreateRateLimitedHttpPipeline(
            operationName: "GetCustomers",
            logger: _logger,
            endpoint: $"/api/customers{queryParams}",
            maxRetries: 5,
            rateLimitWaitSeconds: 5
        );

        // Execute through pipeline
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.GetAsync($"/api/customers{queryParams}", token);
        }, ct);

        // Handle errors
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to get customers after retries: {StatusCode}",
                response.StatusCode);
            return null;
        }

        // Deserialize
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Customer>>(ct);

        _logger.LogInformation(
            "✓ Retrieved {Count} orders (page {Page})",
            result?.Items.Count ?? 0, page);

        return result;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 1: GetCustomerAsync - Standard pipeline with retry + timeout
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get customer by ID with resilience (retry + timeout)
    /// 
    /// Flow:
    /// 1. Pipeline retries on 500/503 errors and network failures
    /// 2. After pipeline completes, check for 404 (not retriable)
    /// 3. EnsureSuccessStatusCode for other errors
    /// 4. Deserialize inside pipeline (part of the operation)
    /// </summary>
    public async Task<Customer?> GetCustomerAsync(string customerId, CancellationToken ct = default)
    {
        // Create pipeline with operation-specific logging
        var pipeline = ResiliencePipelineFactory.CreateHttpPipeline(
            operationName: "GetCustomer",
            logger: _logger,
            endpoint: $"/api/customers/{customerId}",
            maxRetries: 3,
            timeoutSeconds: 5
        );
        
        // Execute HTTP call + deserialization through pipeline
        // Pipeline will retry if: network failure, timeout, 500, 503
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.GetAsync($"/api/customers/{customerId}", token);
        }, ct);
        
        // After pipeline: Handle 404 specifically (semantic error, not transient)
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Customer {CustomerId} not found (404)", customerId);
            return null;
        }
        
        // Throw on other non-success status codes
        response.EnsureSuccessStatusCode();
        
        // Deserialize - this could technically fail, but it's part of "getting a customer"
        var customer = await response.Content.ReadFromJsonAsync<Customer>(ct);
        
        _logger.LogInformation("✓ Retrieved customer: {CustomerName}", customer?.Name);
        return customer;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 2: CheckCreditAsync - Standard pipeline
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Check if customer has sufficient credit for an order
    /// 
    /// Requirements:
    /// - Retry transient errors (500/503)
    /// - Return null if customer not found (404)
    /// - Return CreditCheckResult with approval status
    /// </summary>
    public async Task<CreditCheckResult?> CheckCreditAsync(
        string customerId, 
        decimal orderAmount, 
        CancellationToken ct = default)
    {
        // Create pipeline with order amount in endpoint for better logging
        var pipeline = ResiliencePipelineFactory.CreateHttpPipeline(
            operationName: "CheckCredit",
            logger: _logger,
            endpoint: $"/api/customers/{customerId}/credit-check?amount={orderAmount:C}",
            maxRetries: 3,
            timeoutSeconds: 5
        );
        
        // Execute through pipeline
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.GetAsync(
                $"/api/customers/{customerId}/credit-check?orderAmount={orderAmount}", 
                token);
        }, ct);
        
        // Handle 404 - customer doesn't exist
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("CheckCredit: Customer {CustomerId} not found", customerId);
            return null;
        }
        
        // Throw on other errors
        response.EnsureSuccessStatusCode();
        
        // Deserialize
        var creditCheck = await response.Content.ReadFromJsonAsync<CreditCheckResult>(ct);
        
        // Log approval/decline with context
        if (creditCheck?.Approved == true)
        {
            _logger.LogInformation(
                "✓ Credit APPROVED for {CustomerId}: {Amount:C} (Available: {Available:C})",
                customerId, orderAmount, creditCheck.AvailableCredit);
        }
        else
        {
            _logger.LogWarning(
                "✗ Credit DECLINED for {CustomerId}: {Amount:C} (Available: {Available:C}) - {Reason}",
                customerId, orderAmount, creditCheck?.AvailableCredit, creditCheck?.Reason);
        }
        
        return creditCheck;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 3: UpdateLoyaltyPointsAsync - Fast-fail pipeline (quick operation)
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Update customer loyalty points
    /// Uses fast-fail pipeline (minimal retries) since this is not critical
    /// </summary>
    public async Task<bool> UpdateLoyaltyPointsAsync(
        string customerId, 
        int pointsToAdd, 
        CancellationToken ct = default)
    {
        var pipeline = ResiliencePipelineFactory.CreateFastFailHttpPipeline(
            operationName: "UpdateLoyalty",
            logger: _logger,
            endpoint: $"/api/customers/{customerId}/loyalty",
            timeoutSeconds: 3
        );
        
        var response = await pipeline.ExecuteAsync(async token =>
        {
            return await _httpClient.PutAsJsonAsync(
                $"/api/customers/{customerId}/loyalty",
                new { PointsToAdd = pointsToAdd },
                token);
        }, ct);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "✓ Added {Points} loyalty points to customer {CustomerId}",
                pointsToAdd, customerId);
            return true;
        }
        
        _logger.LogWarning(
            "Failed to update loyalty for {CustomerId}: {StatusCode}",
            customerId, response.StatusCode);
        return false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // METHOD 4: UpdateBalanceAsync - Standard pipeline
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<bool> DecreaseBalanceAsync(
        string customerId,
        DecreaseBalance increaseBalance,
        CancellationToken ct = default
        ) => await UpdateBalanceAsync(customerId, -increaseBalance.Amount, ct);
    public async Task<bool> IncreaseBalanceAsync(
        string customerId,
        IncreaseBalance increaseBalance,
        CancellationToken ct = default
        ) => await UpdateBalanceAsync(customerId, increaseBalance.Amount, ct);


    /// <summary>
    /// Update customer balance (charge or credit)
    /// </summary>
    private async Task<bool> UpdateBalanceAsync(
        string customerId,
        decimal amount,
        CancellationToken ct = default)
    {
        var pipeline = ResiliencePipelineFactory.CreateHttpPipeline(
            operationName: "UpdateBalance",
            logger: _logger,
            endpoint: $"/api/customers/{customerId}/balance",
            maxRetries: 3,
            timeoutSeconds: 5
        );

        var response = await pipeline.ExecuteAsync(async token =>
        {
        return await _httpClient.PostAsJsonAsync(
            $"/api/customers/{customerId}/balance",
            new { Amount = amount },
                token);
        }, ct);
        
        if (response.IsSuccessStatusCode)
        {
            var action = amount > 0 ? "Charged" : "Credited";
            _logger.LogInformation(
                "✓ {Action} {Amount:C} for customer {CustomerId}",
                action, Math.Abs(amount), customerId);
            return true;
        }
        
        _logger.LogWarning(
            "Failed to update balance for {CustomerId}: {StatusCode}",
            customerId, response.StatusCode);
        return false;
    }
    
   
}
