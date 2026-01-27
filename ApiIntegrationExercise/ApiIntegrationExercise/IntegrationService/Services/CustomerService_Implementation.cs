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
    
    // ═══════════════════════════════════════════════════════════════════════════
    // BONUS: Alternative with Circuit Breaker for Critical Operations
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Check credit with circuit breaker for mission-critical operations
    /// Use this if credit checks are essential and you want to fail fast when API is down
    /// </summary>
    public async Task<CreditCheckResult?> CheckCreditWithCircuitBreakerAsync(
        string customerId, 
        decimal orderAmount, 
        CancellationToken ct = default)
    {
        var pipeline = ResiliencePipelineFactory.CreateHttpPipelineWithCircuitBreaker(
            operationName: "CheckCredit-Critical",
            logger: _logger,
            endpoint: $"/api/customers/{customerId}/credit-check",
            maxRetries: 3,
            timeoutSeconds: 5,
            failureThreshold: 0.5,
            breakDurationSeconds: 30
        );
        
        try
        {
            var response = await pipeline.ExecuteAsync(async token =>
            {
                return await _httpClient.GetAsync(
                    $"/api/customers/{customerId}/credit-check?orderAmount={orderAmount}", 
                    token);
            }, ct);
            
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CreditCheckResult>(ct);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Circuit breaker is OPEN - credit check unavailable");
            throw; // Let caller handle circuit breaker being open
        }
    }
}
