namespace LinqPractice.Core;

/// <summary>
/// PROBLEM 3: Query Optimization 
/// 
/// Performance Challenge:
/// You have a slow query in production that's killing database performance.
/// Your task: Optimize it from O(n*m) to O(n+m) using proper LINQ operations.
/// 
/// Scenario:
/// - 10,000 customers
/// - 100,000 orders
/// - Slow version: ~10,000 * 100,000 = 1 billion operations!
/// - Fast version: ~110,000 operations (100x faster!)
/// 
/// 
/// 1. Analyze GetOrderSummaries_Slow() and explain why it's slow
/// 2. Implement GetOrderSummaries_Fast() using GroupBy and Join
/// 
/// 
/// Key Insights:
/// - Slow version: Loops through customers, then filters orders for each (nested loop)
/// - Fast version: Group orders once, join with customers (single pass)
/// </summary>
public class QueryOptimizer
{
    /// <summary>
    /// ❌ SLOW VERSION - O(n*m) complexity
    /// DO NOT MODIFY THIS - It's here for comparison!
    /// </summary>
    public List<OrderSummary> GetOrderSummaries_Slow(List<Order> orders, List<Customer> customers)
    {
        var summaries = new List<OrderSummary>();
        
        // Problem: For each customer, we scan ALL orders
        // If customers = 10K, orders = 100K → 1 billion checks!
        foreach (var customer in customers)
        {
            // This line executes 10,000 times
            var customerOrders = orders
                .Where(o => o.CustomerId == customer.CustomerId)
                .ToList(); // Forces enumeration - scans all 100K orders each time!
            
            summaries.Add(new OrderSummary
            {
                CustomerName = customer.Name,
                OrderCount = customerOrders.Count,
                TotalAmount = customerOrders.Sum(o => o.Amount)
            });
        }
        
        return summaries;
    }
    
    /// <summary>
    /// ✅ FAST VERSION - O(n+m) complexity
    /// YOUR IMPLEMENTATION HERE!
    /// </summary>
    public List<OrderSummary> GetOrderSummaries_Fast(List<Order> orders, List<Customer> customers)
    {
        //Grouping is O(orders)
        var ordersGroup = orders.GroupBy(o => o.CustomerId);
        //Joining is O(customers * 1) cause the group by is implemented as hashset with seraching for some key in O(1)
        var summaries = customers
            .Join(ordersGroup,
            c => c.CustomerId,
            g => g.Key,
            (c, g) => new OrderSummary { CustomerName = c.Name, OrderCount = g.Count(), TotalAmount = g.Sum(o => o.Amount) }
            );
        //total O(customers + orders)
        return summaries.ToList();
       //This solution would again require O(n*m), cause there is no precomputed grouping and it has to recompute each time

        //return customers.GroupJoin(orders, 
        //    c => c.CustomerId, 
        //    o => o.CustomerId, 
        //    (c, orders) => new OrderSummary { CustomerName = c.Name, OrderCount = orders.Count(), TotalAmount = orders.Sum(o => o.Amount) }
        //    )
        //    .ToList();

    }
    
    /// <summary>
    /// BONUS CHALLENGE: What if some customers have no orders?
    /// Implement a version that includes ALL customers, even those with 0 orders.
    /// Hint: Use GroupJoin (left join)
    /// </summary>
    public List<OrderSummary> GetOrderSummaries_IncludeAllCustomers(List<Order> orders, List<Customer> customers)
    {
       
        return customers
            .GroupJoin(orders,
                customer => customer.CustomerId,
                order => order.CustomerId,
                (customer, customerOrders) => new OrderSummary
                {
                    CustomerName = customer.Name,
                    OrderCount = customerOrders.Count(),
                    TotalAmount = customerOrders.Sum(o => o.Amount)
                })
            .ToList();


        throw new NotImplementedException("TODO: Implement GetOrderSummaries_IncludeAllCustomers()");
    }
}

/// <summary>
/// REFLECTION QUESTIONS (answer these in comments):
/// 
/// 1. Time Complexity Analysis:
///    - Slow version: O(?) → Explain
///    - Fast version: O(?) → Explain
/// 
/// 2. Memory Usage:
///    - Does GroupBy create intermediate collections? Is that a problem?
///    - How many times is the orders list enumerated in each version?
/// 
/// 3. Real-World Impact:
///    - If slow version takes 10 seconds, how fast will optimized version be?
///    - At what data size would the difference become noticeable?
/// 
/// 4. Alternative Approaches:
///    - Could you use a Dictionary instead of GroupBy? Would it be faster?
///    - What if orders were sorted by CustomerId - could that help?
/// </summary>
