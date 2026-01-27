using System.Diagnostics;
using System.Runtime.CompilerServices;
using IntegrationService.Models;
using Microsoft.Extensions.Logging;

namespace IntegrationService.Services;

/// <summary>
/// Orchestrates the integration between CustomerApi and OrderApi
/// 
/// Your Task: Implement the business logic that coordinates both APIs
/// 
/// This is where you demonstrate your understanding of:
/// - API consumption and coordination
/// - Error handling and recovery
/// - Business logic implementation
/// - Data validation across systems
/// </summary>
public class IntegrationOrchestrator
    (CustomerService customerService,
     OrderService orderService,
     ILogger<IntegrationOrchestrator> logger
    )
{
    private readonly CustomerService _customerService = customerService;
    private readonly OrderService _orderService = orderService;
    private readonly ILogger<IntegrationOrchestrator> _logger = logger;


    /// <summary>
    /// CHALLENGE 1: Process Pending Orders
    /// 
    /// Your Task:
    /// Process all pending orders by validating them against customer data.
    /// 
    /// Requirements:
    /// 1. Get all pending orders from OrderApi
    /// 2. For each order:
    ///    a. Validate customer exists (CustomerApi)
    ///    b. Check credit limit (CustomerApi)
    ///    c. If valid: Update order status to "Processing"
    ///    d. If invalid: Update order status to "Failed"
    /// 3. Return statistics about processed orders
    /// 4. Continue processing even if some orders fail
    /// 
    /// Tips:
    /// - Process orders sequentially first, then optimize with concurrency if time permits
    /// - Log each step for debugging
    /// - Use try-catch around each order to prevent one failure from stopping everything
    /// </summary>
    /// 
    public async Task<IntegrationResult> ProcessPendingOrdersAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║         CHALLENGE 1: Process Pending Orders             ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");


        var pendingOrders =
            await _orderService.GetOrdersAsync(
            status: "pending",
            page: 1,
            pageSize: 50,
            ct)

            ;
        if (pendingOrders is null)
        {
            _logger.LogError("Cannot retrive the pending orders");
            return new();
        }
        _logger.LogInformation("Found {Count} pending orders", pendingOrders.Items.Count);


        var ordersAndCustomers = await Task.WhenAll(
       pendingOrders.Items.Select(async order => new
       {
           Order = order,
           Customer = await _customerService.GetCustomerAsync(order.CustomerId, ct)
       })
        );
        var ordersWithExistingCustomers = ordersAndCustomers
            .Where(oc => oc.Customer is not null)
            .Select(oc => new { oc.Order, Customer = oc.Customer! })
            .ToList();

        var ordersWithMissingCustomers = ordersAndCustomers
              .Where(oc => oc.Customer is null)
              .ToList()
              ;

        await Task.WhenAll(ordersWithMissingCustomers.Select(oc =>
        {
            _logger.LogError(
                "Order {OrderId} has invalid customer {CustomerId}",
                oc.Order.OrderId, oc.Order.CustomerId);

            return _orderService.UpdateOrderStatusAsync(oc.Order.OrderId, "Failed", ct);
        }));

        var creditChecks = await Task.WhenAll(
           ordersWithExistingCustomers.Select(async oc => new
           {
               oc.Order,
               CreditResult = await _customerService.CheckCreditAsync(
                   oc.Customer.CustomerId,
                   oc.Order.TotalAmount,
                   ct)
           })
       );

        // Step 6: Separate approved from declined based on credit
        var ordersWithNullCredit = creditChecks
            .Where(c => c.CreditResult is null)
            .Select(c => c.Order)
            .ToList();

        var ordersWithDeclinedCredit = creditChecks
            .Where(c => c.CreditResult?.Approved is false)
            .Select(c => (c.Order, c.CreditResult!.Reason))
            .ToList();

        var approvedOrders = creditChecks
            .Where(c => c.CreditResult?.Approved == true)
            .Select(c => c.Order)
            .ToList();

        foreach (var order in ordersWithNullCredit)
        {
            _logger.LogError("Credit check returned null for order {OrderId}", order.OrderId);
        }

        foreach (var oc in ordersWithDeclinedCredit)
        {
            await _orderService.UpdateOrderStatusAsync(oc.Order.OrderId, "Failed", ct);
        }


        //Mark approved orders as processing 
        await Task.WhenAll(approvedOrders.Select(order =>
        {
            _logger.LogInformation("Marking order {OrderId} as Processing", order.OrderId);
            return _orderService.UpdateOrderStatusAsync(order.OrderId, "Processing", ct);
        }));

        stopwatch.Stop();

        var failedOrdersIds = ordersWithMissingCustomers.Select(oc => oc.Order.OrderId)
            .Concat(ordersWithDeclinedCredit.Select(o => o.Order.OrderId))
            .Concat(ordersWithNullCredit.Select(o => o.OrderId))
            .ToList();

        var errorMessages = ordersWithMissingCustomers
        .Select(o => $"Order {o.Order.OrderId}: Customer {o.Order.CustomerId} not found")
        .Concat(ordersWithNullCredit.Select(o => $"Order {o.OrderId}: Credit check failed"))
        .Concat(ordersWithDeclinedCredit.Select(d => $"Order {d.Order.OrderId}: {d.Reason}"))
        .ToList();

        return new IntegrationResult
        {
            TotalOrdersProcessed = pendingOrders.Items.Count,
            SuccessfulOrders = approvedOrders.Count,
            FailedOrders = failedOrdersIds.Count,
            Duration = stopwatch.Elapsed,
            ErrorMessages = errorMessages,
            FailedOrderIds = failedOrdersIds
        };



    }

  
    public async Task<IntegrationResult> CompleteOrdersAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║    CHALLENGE 2: Complete Orders & Update Customers      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        // TODO: Implement this method!

        var processingOrders = (await _orderService.GetOrdersAsync(
            status: "processing",
            page: 1,
            pageSize: 50,
            ct))?.Items;
        if (processingOrders is null)
        {
            _logger.LogInformation("No orders to be completed");
            return new();
        }
        foreach (var order in processingOrders)
        {
            var BalanceUpdated =
                await _customerService.DecreaseBalanceAsync(order.CustomerId, new(order.TotalAmount), ct);
            if (!BalanceUpdated)
            {
                _logger.LogWarning("Cannot Update balance for Customer {CustomerId} having Order {OrderId}", order.CustomerId, order.OrderId);
            }
          //  _customerService.UpdateLoyaltyPointsAsync
        }
        

        stopwatch.Stop();

        throw new NotImplementedException("TODO: Implement CompleteOrdersAsync");
    }

    
    public async Task<IntegrationResult> SyncCustomerLoyaltyAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       CHALLENGE 3: Sync Customer Loyalty Tiers          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        // TODO: Implement this method!

        stopwatch.Stop();

        throw new NotImplementedException("TODO: Implement SyncCustomerLoyaltyAsync");
    }


}
