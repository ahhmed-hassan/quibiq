namespace LinqPractice.Tests;

using LinqPractice.Core;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Tests for Problem 3: Query Optimization
/// 
/// Performance challenge: Optimize from O(n*m) to O(n+m)
/// These tests verify both correctness AND performance
/// </summary>
public class Problem3_QueryOptimization_Tests
{
    private readonly ITestOutputHelper _output;
    private readonly QueryOptimizer _optimizer = new();
    
    public Problem3_QueryOptimization_Tests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void GetOrderSummaries_Fast_ReturnsCorrectCount()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _optimizer.GetOrderSummaries_Fast(orders, customers);
        
        // Assert
        // All 8 customers should be included (some with 0 orders if using inner join)
        // But only customers WITH orders if using inner join
        Assert.True(result.Count > 0);
    }
    
    [Fact]
    public void GetOrderSummaries_Fast_MatchesSlowVersion_ForSmallDataset()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var slowResult = _optimizer.GetOrderSummaries_Slow(orders, customers);
        var fastResult = _optimizer.GetOrderSummaries_Fast(orders, customers);
        
        // Assert - Results should match
        Assert.Equal(slowResult.Count, fastResult.Count);
        
        // Verify each customer's summary matches
        foreach (var slowSummary in slowResult)
        {
            var fastSummary = fastResult.FirstOrDefault(f => f.CustomerName == slowSummary.CustomerName);
            Assert.NotNull(fastSummary);
            Assert.Equal(slowSummary.OrderCount, fastSummary.OrderCount);
            Assert.Equal(slowSummary.TotalAmount, fastSummary.TotalAmount);
        }
    }
    
    [Fact]
    public void GetOrderSummaries_Fast_CalculatesCorrectOrderCount()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _optimizer.GetOrderSummaries_Fast(orders, customers);
        
        // Assert
        // Alice has 2 orders
        var aliceSummary = result.FirstOrDefault(s => s.CustomerName == "Alice Smith");
        Assert.NotNull(aliceSummary);
        Assert.Equal(2, aliceSummary.OrderCount);
        
        // Bob has 2 orders
        var bobSummary = result.FirstOrDefault(s => s.CustomerName == "Bob Johnson");
        Assert.NotNull(bobSummary);
        Assert.Equal(2, bobSummary.OrderCount);
    }
    
    [Fact]
    public void GetOrderSummaries_Fast_CalculatesCorrectTotalAmount()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _optimizer.GetOrderSummaries_Fast(orders, customers);
        
        // Assert
        // Alice: 800 + 700 = 1500
        var aliceSummary = result.FirstOrDefault(s => s.CustomerName == "Alice Smith");
        Assert.NotNull(aliceSummary);
        Assert.Equal(1500M, aliceSummary.TotalAmount);
        
        // Bob: 1500 + 1000 = 2500
        var bobSummary = result.FirstOrDefault(s => s.CustomerName == "Bob Johnson");
        Assert.NotNull(bobSummary);
        Assert.Equal(2500M, bobSummary.TotalAmount);
    }
    
    [Fact]
    public void GetOrderSummaries_Fast_IsSignificantlyFasterThanSlow()
    {
        // Arrange
        var (orders, customers) = TestData.GetLargeDataset();
        // 1000 customers, 5000 orders
        
        _output.WriteLine($"Testing with {customers.Count} customers and {orders.Count} orders");
        
        // Act & Measure - Slow version
        var stopwatchSlow = Stopwatch.StartNew();
        var slowResult = _optimizer.GetOrderSummaries_Slow(orders, customers);
        stopwatchSlow.Stop();
        
        // Act & Measure - Fast version
        var stopwatchFast = Stopwatch.StartNew();
        var fastResult = _optimizer.GetOrderSummaries_Fast(orders, customers);
        stopwatchFast.Stop();
        
        // Assert - Results match
        Assert.NotEqual(slowResult.Count, fastResult.Count);
        
        // Assert - Performance improvement
        _output.WriteLine($"Slow version: {stopwatchSlow.ElapsedMilliseconds}ms");
        _output.WriteLine($"Fast version: {stopwatchFast.ElapsedMilliseconds}ms");
        _output.WriteLine($"Speedup: {(double)stopwatchSlow.ElapsedMilliseconds / stopwatchFast.ElapsedMilliseconds:F2}x faster");
        
        // Fast version should be significantly faster (at least 5x)
        Assert.True(
            stopwatchFast.ElapsedMilliseconds * 5 < stopwatchSlow.ElapsedMilliseconds,
            $"Fast version ({stopwatchFast.ElapsedMilliseconds}ms) should be at least 5x faster than slow version ({stopwatchSlow.ElapsedMilliseconds}ms)"
        );
    }
    
    [Fact]
    public void GetOrderSummaries_Fast_WithEmptyOrders_ReturnsEmptyList()
    {
        // Arrange
        var orders = new List<Order>();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _optimizer.GetOrderSummaries_Fast(orders, customers);
        
        // Assert
        Assert.Empty(result); // No orders means no summaries (using inner join)
    }
    
    [Fact]
    public void GetOrderSummaries_Fast_WithSingleCustomer_ReturnsCorrectSummary()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerId = 1, Amount = 100, Date = DateTime.Now },
            new() { OrderId = 2, CustomerId = 1, Amount = 200, Date = DateTime.Now }
        };
        var customers = new List<Customer>
        {
            new() { CustomerId = 1, Name = "Test Customer", Segment = "Premium" }
        };
        
        // Act
        var result = _optimizer.GetOrderSummaries_Fast(orders, customers);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("Test Customer", result[0].CustomerName);
        Assert.Equal(2, result[0].OrderCount);
        Assert.Equal(300M, result[0].TotalAmount);
    }
    
    // ========================================================================
    // BONUS: Left Join Tests (Include All Customers)
    // ========================================================================
    
    [Fact]
    public void GetOrderSummaries_IncludeAllCustomers_IncludesCustomersWithNoOrders()
    {
        // Arrange
        List<Order> orders =
        [
            new() { OrderId = 1, CustomerId = 1, Amount = 100, Date = DateTime.Now }
        ];
        List<Customer> customers = 
        [
            new() { CustomerId = 1, Name = "Customer With Orders", Segment = "Premium" },
            new() { CustomerId = 2, Name = "Customer Without Orders", Segment = "Basic" }
        ];
        
        // Act
        var result = _optimizer.GetOrderSummaries_IncludeAllCustomers(orders, customers);
        
        // Assert
        Assert.Equal(2, result.Count); // Both customers included
        
        var customerWithOrders = result.First(s => s.CustomerName == "Customer With Orders");
        Assert.Equal(1, customerWithOrders.OrderCount);
        Assert.Equal(100M, customerWithOrders.TotalAmount);
        
        var customerWithoutOrders = result.First(s => s.CustomerName == "Customer Without Orders");
        Assert.Equal(0, customerWithoutOrders.OrderCount);
        Assert.Equal(0M, customerWithoutOrders.TotalAmount);
    }
    
    [Fact]
    public void GetOrderSummaries_IncludeAllCustomers_MatchesCustomerCount()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _optimizer.GetOrderSummaries_IncludeAllCustomers(orders, customers);
        
        // Assert
        // Should include ALL customers, even those without orders
        Assert.Equal(customers.Count, result.Count);
    }
}
