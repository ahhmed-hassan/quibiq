namespace LinqPractice.Tests;

using LinqPractice.Core;
using Xunit;

/// <summary>
/// Tests for Problem 1: Customer Spending Analysis
/// 
/// These tests will FAIL initially - that's expected!
/// Your goal: Implement the methods until all tests pass ✅
/// </summary>
public class Problem1_CustomerSpending_Tests
{
    private readonly CustomerSpendingAnalyzer _analyzer = new();
    
    [Fact]
    public void AnalyzeSpending_WithTestData_ReturnsCorrectNumberOfSegments()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _analyzer.AnalyzeSpending(orders, customers);
        
        // Assert
        Assert.Equal(3, result.Count); // Premium, Standard, Basic
    }
    
    [Fact]
    public void AnalyzeSpending_FiltersCustomersOver1000Euro()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _analyzer.AnalyzeSpending(orders, customers);
        
        // Assert
        // Premium: Alice (1500), Bob (2500), Grace (3000) = 3 customers
        var premiumSegment = result.FirstOrDefault(r => r.Segment == "Premium");
        Assert.NotNull(premiumSegment);
        Assert.Equal(3, premiumSegment.CustomerCount);
        
        // Standard: Only Charlie (1200) qualifies, Diana (800) excluded
        var standardSegment = result.FirstOrDefault(r => r.Segment == "Standard");
        Assert.NotNull(standardSegment);
        Assert.Equal(1, standardSegment.CustomerCount);
        
        // Basic: Only Frank (1100) qualifies, Eve (500) excluded
        var basicSegment = result.FirstOrDefault(r => r.Segment == "Basic");
        Assert.NotNull(basicSegment);
        Assert.Equal(1, basicSegment.CustomerCount);
    }
    
    [Fact]
    public void AnalyzeSpending_CalculatesCorrectTotalRevenue()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _analyzer.AnalyzeSpending(orders, customers);
        
        // Assert
        var premiumSegment = result.First(r => r.Segment == "Premium");
        // Alice: 800+700=1500, Bob: 1500+1000=2500, Grace: 2000+1000=3000
        // Total: 7000
        Assert.Equal(7000M, premiumSegment.TotalRevenue);
        
        var standardSegment = result.First(r => r.Segment == "Standard");
        // Charlie: 600+600=1200
        Assert.Equal(1200M, standardSegment.TotalRevenue);
        
        var basicSegment = result.First(r => r.Segment == "Basic");
        // Frank: 600+500=1100
        Assert.Equal(1100M, basicSegment.TotalRevenue);
    }
    
    [Fact]
    public void AnalyzeSpending_CalculatesCorrectAverageSpending()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _analyzer.AnalyzeSpending(orders, customers);
        
        // Assert
        var premiumSegment = result.First(r => r.Segment == "Premium");
        // (1500 + 2500 + 3000) / 3 = 2333.33
        Assert.Equal(2333.33M, premiumSegment.AverageSpending, 2); // 2 decimal places
        
        var standardSegment = result.First(r => r.Segment == "Standard");
        // 1200 / 1 = 1200
        Assert.Equal(1200M, standardSegment.AverageSpending);
        
        var basicSegment = result.First(r => r.Segment == "Basic");
        // 1100 / 1 = 1100
        Assert.Equal(1100M, basicSegment.AverageSpending);
    }
    
    [Fact]
    public void AnalyzeSpending_SortsByAverageSpendingDescending()
    {
        // Arrange
        var orders = TestData.GetOrders();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _analyzer.AnalyzeSpending(orders, customers);
        
        // Assert
        Assert.Equal("Premium", result[0].Segment); // Highest average: 2333.33
        Assert.Equal("Standard", result[1].Segment); // Second: 1200
        Assert.Equal("Basic", result[2].Segment); // Lowest: 1100
    }
    
    [Fact]
    public void AnalyzeSpending_WithEmptyOrders_ReturnsEmptyList()
    {
        // Arrange
        var orders = new List<Order>();
        var customers = TestData.GetCustomers();
        
        // Act
        var result = _analyzer.AnalyzeSpending(orders, customers);
        
        // Assert
        Assert.Empty(result); // No customers have spent > 1000€
    }
    
    [Fact]
    public void AnalyzeSpending_WithSingleHighValueCustomer_ReturnsOneSegment()
    {
        // Arrange
        var orders = new List<Order>
        {
            new() { OrderId = 1, CustomerId = 1, Amount = 1500, Date = DateTime.Now }
        };
        var customers = new List<Customer>
        {
            new() { CustomerId = 1, Name = "Test Customer", Segment = "Premium" }
        };
        
        // Act
        var result = _analyzer.AnalyzeSpending(orders, customers);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("Premium", result[0].Segment);
        Assert.Equal(1, result[0].CustomerCount);
        Assert.Equal(1500M, result[0].TotalRevenue);
        Assert.Equal(1500M, result[0].AverageSpending);
    }
}
