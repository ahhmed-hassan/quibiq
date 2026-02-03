using System.Net;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace IntegrationService.Infrastructure;

/// <summary>
/// Factory for creating composable Polly resilience pipelines with structured logging
/// </summary>
public static class ResiliencePipelineFactory
{
    // ═══════════════════════════════════════════════════════════════════════════
    // BUILDING BLOCK 1: Standard Retry Options (Transient HTTP Errors)
    // ═══════════════════════════════════════════════════════════════════════════
    
    private static RetryStrategyOptions<HttpResponseMessage> GetStandardRetryOptions(
        string operationName,
        ILogger logger,
        string? endpoint = null,
        int maxRetries = 3)
    {
        return new RetryStrategyOptions<HttpResponseMessage>
        {
            // What to retry: Network failures + 5xx errors
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()              // Network failures
                .Handle<TimeoutRejectedException>()          // Timeout
                .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError)      // 500
                .HandleResult(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)       // 503
                .HandleResult(r => r.StatusCode == HttpStatusCode.RequestTimeout),          // 408
            
            MaxRetryAttempts = maxRetries,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,  // 1s, 2s, 4s
            UseJitter = true,  // Add randomness to prevent thundering herd
            
            // Structured logging
            OnRetry = args =>
            {
                var statusCode = args.Outcome.Result?.StatusCode;
                var exceptionType = args.Outcome.Exception?.GetType().Name;
                var endpointInfo = string.IsNullOrEmpty(endpoint) ? "" : $" [{endpoint}]";
                var error = statusCode?.ToString() ?? exceptionType ?? "Unknown";
                
                logger.LogWarning(
                    "[{Operation}]{Endpoint} Retry {Attempt}/{Max} after {Delay:F1}s → {Error}",
                    operationName, endpointInfo, args.AttemptNumber, maxRetries,
                    args.RetryDelay.TotalSeconds, error);
                
                return ValueTask.CompletedTask;
            }
        };
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // BUILDING BLOCK 2: Rate Limit Retry Options (Handles 429)
    // ═══════════════════════════════════════════════════════════════════════════
    
    private static RetryStrategyOptions<HttpResponseMessage> GetRateLimitRetryOptions(
        string operationName,
        ILogger logger,
        string? endpoint = null,
        int maxRetries = 5,
        int rateLimitWaitSeconds = 5)
    {
        return new RetryStrategyOptions<HttpResponseMessage>
        {
            // Retry on 429 specifically
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests),
            
            MaxRetryAttempts = maxRetries,
            
            // Custom delay: longer for rate limits
            Delay = TimeSpan.FromSeconds(rateLimitWaitSeconds),
                        
            // Structured logging with emoji for rate limits
            OnRetry = args =>
            {
                var endpointInfo = string.IsNullOrEmpty(endpoint) ? "" : $" [{endpoint}]";
                
                logger.LogWarning(
                    "🚦 [{Operation}]{Endpoint} Retry {Attempt}/{Max} after {Delay:F1}s → Rate limited",
                    operationName, endpointInfo, args.AttemptNumber, maxRetries,
                    args.RetryDelay.TotalSeconds);
                
                return ValueTask.CompletedTask;
            }
        };
    }
    
    
    private static TimeoutStrategyOptions GetTimeoutStrategyOptions(int timeoutSeconds, ILogger logger, string operationName, string? endpoint = null)
        => new ()
    {
        Timeout = TimeSpan.FromSeconds(timeoutSeconds),
        OnTimeout = args =>
        {
            var endpointInfo = string.IsNullOrEmpty(endpoint) ? "" : $" [{endpoint}]";
            logger.LogWarning(
                "⏱️  [{Operation}]{Endpoint} Timeout after {Timeout}s",
                operationName, endpointInfo, args.Timeout.TotalSeconds);
            return ValueTask.CompletedTask;
        }
    };
    

    // ═══════════════════════════════════════════════════════════════════════════
    // COMPOSED PIPELINE 1: Standard HTTP Pipeline
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a standard resilience pipeline with retry + timeout
    /// Use for most HTTP operations with transient failures
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateHttpPipeline(
        string operationName,
        ILogger logger,
        string? endpoint = null,
        int maxRetries = 3,
        int timeoutSeconds = 5)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(GetStandardRetryOptions(operationName, logger, endpoint, maxRetries))
            //.AddTimeout(new TimeoutStrategyOptions
            //{
            //    Timeout = TimeSpan.FromSeconds(timeoutSeconds),
            //    OnTimeout = args =>
            //    {
            //        var endpointInfo = string.IsNullOrEmpty(endpoint) ? "" : $" [{endpoint}]";
            //        logger.LogWarning(
            //            "⏱️  [{Operation}]{Endpoint} Timeout after {Timeout}s",
            //            operationName, endpointInfo, args.Timeout.TotalSeconds);
            //        return ValueTask.CompletedTask;
            //    }
            //})
            .AddTimeout(GetTimeoutStrategyOptions(timeoutSeconds, logger, operationName, endpoint))
            .Build();
    }
    
   
    // ═══════════════════════════════════════════════════════════════════════════
    // COMPOSED PIPELINE 3: Rate-Limited HTTP Pipeline
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Creates a resilience pipeline that handles both transient errors AND rate limiting (429)
    /// Use for APIs that return 429 Too Many Requests
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateRateLimitedHttpPipeline(
        string operationName,
        ILogger logger,
        string? endpoint = null,
        int maxRetries = 5,
        int rateLimitWaitSeconds = 5)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            // First: Standard retry for 5xx errors
            .AddRetry(GetStandardRetryOptions(operationName, logger, endpoint, 3))
            // Second: Rate limit retry for 429
            .AddRetry(GetRateLimitRetryOptions(operationName, logger, endpoint, maxRetries, rateLimitWaitSeconds))
            .AddTimeout(GetTimeoutStrategyOptions(10, logger, operationName, endpoint))
            .Build();
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // COMPOSED PIPELINE 4: Fast-Fail Pipeline
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Creates a fast-fail pipeline with minimal retries
    /// Use for health checks or non-critical operations
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateFastFailHttpPipeline(
        string operationName,
        ILogger logger,
        string? endpoint = null,
        int timeoutSeconds = 2)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode == HttpStatusCode.ServiceUnavailable),
                
                MaxRetryAttempts = 1,  // Only 1 retry
                Delay = TimeSpan.FromMilliseconds(500),
                
                OnRetry = args =>
                {
                    var endpointInfo = string.IsNullOrEmpty(endpoint) ? "" : $" [{endpoint}]";
                    logger.LogWarning(
                        "⚡ [{Operation}]{Endpoint} Quick retry after {Delay}ms",
                        operationName, endpointInfo, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(timeoutSeconds))
            .Build();
    }
}
