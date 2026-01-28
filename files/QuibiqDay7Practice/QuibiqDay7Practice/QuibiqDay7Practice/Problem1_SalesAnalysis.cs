namespace QuibiqDay7Practice.Problem1;

/// <summary>
/// Represents a single sale transaction from the ERP system
/// </summary>
public record SaleTransaction(
    string CustomerId,
    string CustomerName,
    decimal Amount,
    DateTime Date);

/// <summary>
/// Summary of a customer's sales performance
/// </summary>
public record CustomerSalesSummary(
    string CustomerId,
    string CustomerName,
    decimal TotalSales,
    int OrderCount
    ){
    public decimal AverageOrderValue => TotalSales / OrderCount; 
};

/// <summary>
/// Service to analyze sales data and generate customer insights
/// </summary>
public interface ISalesAnalyzer
{
    /// <summary>
    /// Analyzes transactions and returns summary for each customer
    /// </summary>
    List<CustomerSalesSummary> AnalyzeCustomerSales(List<SaleTransaction> transactions);
    
    /// <summary>
    /// Returns top N customers by total sales
    /// </summary>
    List<CustomerSalesSummary> GetTopCustomers(List<SaleTransaction> transactions, int topN);
    
    /// <summary>
    /// Calculates total revenue across all transactions
    /// </summary>
    decimal CalculateTotalRevenue(List<SaleTransaction> transactions);
}

public class SalesAnalyzer : ISalesAnalyzer
{
    public List<CustomerSalesSummary> AnalyzeCustomerSales(List<SaleTransaction> transactions)
    {
        var customerGrouping = transactions.GroupBy(t => t.CustomerId);
        var result = customerGrouping.Select(cg =>
            new CustomerSalesSummary
            (
                CustomerId : cg.Key,
                CustomerName : cg.First().CustomerName,
                TotalSales : cg.Sum(x => x.Amount),
                OrderCount : cg.Count()
            )
        );

        return result.ToList();
    }

    public decimal CalculateTotalRevenue(List<SaleTransaction> transactions)
        => transactions.Sum(transaction => transaction.Amount);

    public List<CustomerSalesSummary> GetTopCustomers(List<SaleTransaction> transactions, int topN)
    {
        return AnalyzeCustomerSales(transactions)
            .OrderByDescending(customerSummary => customerSummary.TotalSales)
            .Take(topN)
            .ToList();
    }
}
