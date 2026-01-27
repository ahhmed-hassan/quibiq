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
    private const int AmountPerPoint = 10; 


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
            Duration = stopwatch.Elapsed,
            ErrorMessages = errorMessages,
            FailedOrderIds = failedOrdersIds
        };



    }

    /// <summary>
    /// CHALLENGE 2: Complete Orders and Update Customer Data
    /// 
    /// Your Task:
    /// Move orders from "Processing" to "Completed" and update customer loyalty/balance.
    /// 
    /// Requirements:
    /// 1. Get all "Processing" orders
    /// 2. For each order:
    ///    a. Update customer balance (charge them) - CustomerApi
    ///    b. Award loyalty points (€10 = 1 point) - CustomerApi
    ///    c. Update order status to "Completed" - OrderApi
    /// 3. Handle errors gracefully (retry failed operations)
    /// 
    /// Business Rules:
    /// - Loyalty Points: 1 point per €10 spent
    /// - Balance Update: Add order amount to current balance
    /// - If ANY step fails, log error but continue with other orders
    /// </summary>
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
        var ordersAndBalancesUpdates = await Task.WhenAll(processingOrders.Select(async order =>
         new
         {
             order,
             IsBalanceUpdated = await _customerService.DecreaseBalanceAsync(order.CustomerId, new(order.TotalAmount), ct)
         }
        )
         );
        var failedUpdatedBalances = ordersAndBalancesUpdates.Where(ob => !ob.IsBalanceUpdated).ToList();
        var updatedBalanceOrders = ordersAndBalancesUpdates.Where(ob => ob.IsBalanceUpdated).Select(ob => ob.order);

        foreach (var failedOrderbalance in failedUpdatedBalances)
        {
            _logger.LogError("Could not update Balance Order {Order.Id}", failedOrderbalance.order.OrderId);
        }

        var ordersRoyalityUpdates = await Task.WhenAll(updatedBalanceOrders.Select(async order =>
        {
            var pointsEarned = (int)order.TotalAmount / AmountPerPoint;
            return new
            {
                order.OrderId,
                LoyalityUpdated = await _customerService.UpdateLoyaltyPointsAsync(order.CustomerId, pointsEarned, ct)
            };
        }
        ));
        var updatedRoyalityOrders = ordersRoyalityUpdates.Where(ou => ou.LoyalityUpdated);
        var failedRoyalityUpdated = ordersRoyalityUpdates.Where(ou => !ou.LoyalityUpdated).ToList();
        foreach (var failedRoyalityOrder in failedRoyalityUpdated)
        {
            _logger.LogError("Could not update Loyality Order {Order.Id}", failedRoyalityOrder.OrderId);
        }

        await Task.WhenAll(updatedRoyalityOrders.Select(async ou =>
            await _orderService.UpdateOrderStatusAsync(ou.OrderId, "Completed", ct)));

        IEnumerable<string> errorMesasges = failedRoyalityUpdated.Select(ou => $"Loyality Update failed for Order {ou.OrderId}")
            .Concat(failedUpdatedBalances.Select(ou => $"balance Update failed for Order {ou.order.OrderId}"));

        ;

        stopwatch.Stop();
        return new()
        {
            TotalOrdersProcessed = processingOrders.Count(),
            ErrorMessages = errorMesasges.ToList(),
            FailedOrderIds = failedRoyalityUpdated.Select(ou => ou.OrderId)
            .Concat(failedUpdatedBalances.Select(ou => ou.order.OrderId)).ToList(),
            Duration = stopwatch.Elapsed,
        };

    }

    /// <summary>
    /// CHALLENGE 3: Sync Customer Loyalty Tiers
    /// 
    /// Your Task:
    /// Calculate total order value per customer and update loyalty points accordingly.
    /// 
    /// This simulates a "nightly batch job" that reconciles data between systems.
    /// </summary>
    public async Task<IntegrationResult> SyncCustomerLoyaltyAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       CHALLENGE 3: Sync Customer Loyalty Tiers          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

        // TODO: Implement this method!
        var customers = (await _customerService.GetCustomersAsync(1, 50, ct))?.Items;
        if (customers is null)
        {
            _logger.LogCritical("No customers Found");
            return new();
        }
        var customersWithOrders = (
            await Task.WhenAll(customers.Select(async customer =>
            new
            {
                Customer = customer,
                Orders = await _orderService.GetOrdersByCustomerAsync(customer.CustomerId, ct),

            }
            ))
            ).Where(co => co.Orders.Any()).ToList(); 
           ;

        var loyaltyUpdates = await Task.WhenAll(customersWithOrders.Select(async co =>
        {
            // Calculate expected loyalty based on order history
            var expectedPoints = (int)(co.Orders.Sum(o => o.TotalAmount) / AmountPerPoint);
            var currentPoints = co.Customer.LoyaltyPoints;
            var pointsDifference = expectedPoints - currentPoints;

            // Only update if out of sync
            if (pointsDifference == 0)
            {
                _logger.LogInformation(
                    "Customer {CustomerId} loyalty already in sync ({Points} points)",
                    co.Customer.CustomerId, currentPoints);
                return new
                {
                    co.Customer.CustomerId,
                    co.Orders,
                    LoyaltyUpdated = true,
                    WasInSync = true
                };
            }

            _logger.LogInformation(
                "Customer {CustomerId}: Expected {Expected} points, has {Current} points, difference: {Diff}",
                co.Customer.CustomerId, expectedPoints, currentPoints, pointsDifference);

            var updated = await _customerService.UpdateLoyaltyPointsAsync(
                co.Customer.CustomerId,
                pointsDifference,
                ct);

            return new
            {
                co.Customer.CustomerId,
                co.Orders,
                LoyaltyUpdated = updated,
                WasInSync = false
            };
        }));

        stopwatch.Stop();

        // Step 4: Build results
        var failedUpdates = loyaltyUpdates
            .Where(x => !x.LoyaltyUpdated)
            .ToList();

        var successfulUpdates = loyaltyUpdates
            .Where(x => x.LoyaltyUpdated && !x.WasInSync)
            .ToList();

        foreach (var failed in failedUpdates)
        {
            _logger.LogError(
                "Could not update loyalty for Customer {CustomerId}",
                failed.CustomerId);
        }

        foreach (var success in successfulUpdates)
        {
            _logger.LogInformation(
                "✓ Loyalty synced for Customer {CustomerId}",
                success.CustomerId);
        }

        var errorMessages = failedUpdates
            .Select(f => $"Loyalty update failed for Customer {f.CustomerId}")
            .ToList();

        var failedOrderIds = failedUpdates
            .SelectMany(f => f.Orders.Select(o => o.OrderId))
            .ToList();

        stopwatch.Stop();
        
        return new()
        {
            TotalOrdersProcessed = customersWithOrders.Sum(c => c.Orders.Count),
            Duration = stopwatch.Elapsed,
            ErrorMessages = errorMessages,
            FailedOrderIds = failedOrderIds.ToList()
        };


    }


}
