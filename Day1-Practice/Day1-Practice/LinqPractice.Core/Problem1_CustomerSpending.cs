namespace LinqPractice.Core;

/// <summary>

/// Business Context:
/// Your company wants to identify high-value customers and understand 
/// spending patterns across different customer segments.
/// 
/// Flow of the method: 
/// Implement AnalyzeSpending() that:
/// 1. Joins orders with customers
/// 2. Groups by customer, calculates total spending
/// 3. Filters customers who spent > 1000€
/// 4. Groups by segment (Premium/Standard/Basic)
/// 5. Calculates aggregates per segment
/// 6. Returns sorted by average spending (descending)
/// 
/// Example Input:
/// Orders: [
///   { CustomerId: 1, Amount: 500 },
///   { CustomerId: 1, Amount: 600 },  // Total: 1100 (included)
///   { CustomerId: 2, Amount: 200 }   // Total: 200 (excluded)
/// ]
/// Customers: [
///   { CustomerId: 1, Segment: "Premium" },
///   { CustomerId: 2, Segment: "Basic" }
/// ]
/// 
/// Expected Output: [
///   { Segment: "Premium", CustomerCount: 1, AverageSpending: 1100, TotalRevenue: 1100 }
/// ]
/// </summary>
public class CustomerSpendingAnalyzer
{
    public List<SpendingReport> AnalyzeSpending(List<Order> orders, List<Customer> customers)
    {
     
        var customersGroup = customers.GroupJoin(orders,
            c => c.CustomerId,
            o => o.CustomerId,
            (customer, orders) => new
            {
                customer,
                orders,
                TotalSpending = orders.Sum(o => o.Amount)
            }

            );
        var richCustomers= customersGroup
            .Where(group => group.TotalSpending >1_000);

        var richCustomerGroupedBySegment = richCustomers
            .GroupBy(x => x.customer.Segment);

        var report = richCustomerGroupedBySegment.Select(g =>
        new SpendingReport {
            Segment = g.Key, 
            CustomerCount = g.Count(),
            TotalRevenue= g.Sum(x=> x.TotalSpending) }
        );
        

        return report.OrderByDescending(r => r.AverageSpending).ToList();
    }
}
