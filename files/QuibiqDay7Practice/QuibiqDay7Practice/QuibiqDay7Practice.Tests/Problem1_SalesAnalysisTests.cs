using FluentAssertions;
using QuibiqDay7Practice.Problem1;
using Xunit;

namespace QuibiqDay7Practice.Tests;

public class Problem1_SalesAnalysisTests
{
    // TODO: Implement SalesAnalyzer class that implements ISalesAnalyzer
    
    [Fact]
    public void AnalyzeCustomerSales_WithMultipleTransactions_GroupsByCustomerAndCalculatesTotals()
    {
        // Arrange
        var transactions = new List<SaleTransaction>
        {
            new("C001", "Acme Corp", 1000m, new DateTime(2026, 1, 15)),
            new("C001", "Acme Corp", 1500m, new DateTime(2026, 1, 20)),
            new("C002", "Global Tech", 2000m, new DateTime(2026, 1, 16)),
            new("C003", "Innovation Ltd", 500m, new DateTime(2026, 1, 17))
        };
        
        var analyzer = CreateAnalyzer();
        
        // Act
        var result = analyzer.AnalyzeCustomerSales(transactions);
        
        // Assert
        result.Should().HaveCount(3);
        
        var acmeSummary = result.First(x => x.CustomerId == "C001");
        acmeSummary.CustomerName.Should().Be("Acme Corp");
        acmeSummary.TotalSales.Should().Be(2500m);
        acmeSummary.OrderCount.Should().Be(2);
        acmeSummary.AverageOrderValue.Should().Be(1250m);
        
        var globalSummary = result.First(x => x.CustomerId == "C002");
        globalSummary.TotalSales.Should().Be(2000m);
        globalSummary.OrderCount.Should().Be(1);
        globalSummary.AverageOrderValue.Should().Be(2000m);
    }
    
    [Fact]
    public void AnalyzeCustomerSales_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        
        // Act
        var result = analyzer.AnalyzeCustomerSales(new List<SaleTransaction>());
        
        // Assert
        result.Should().BeEmpty();
    }
    
    [Fact]
    public void GetTopCustomers_ReturnsCustomersOrderedByTotalSales()
    {
        // Arrange
        var transactions = new List<SaleTransaction>
        {
            new("C001", "Acme Corp", 1000m, new DateTime(2026, 1, 15)),
            new("C002", "Global Tech", 5000m, new DateTime(2026, 1, 16)),
            new("C003", "Innovation Ltd", 3000m, new DateTime(2026, 1, 17)),
            new("C001", "Acme Corp", 500m, new DateTime(2026, 1, 18)),
            new("C003", "Innovation Ltd", 1500m, new DateTime(2026, 1, 19))
        };
        
        var analyzer = CreateAnalyzer();
        
        // Act
        var result = analyzer.GetTopCustomers(transactions, 2);
        
        // Assert
        result.Should().HaveCount(2);
        result[0].CustomerId.Should().Be("C002"); // 5000 total
        result[0].TotalSales.Should().Be(5000m);
        result[1].CustomerId.Should().Be("C003"); // 4500 total
        result[1].TotalSales.Should().Be(4500m);
    }
    
    [Fact]
    public void GetTopCustomers_WhenTopNExceedsCustomerCount_ReturnsAllCustomers()
    {
        // Arrange
        var transactions = new List<SaleTransaction>
        {
            new("C001", "Acme Corp", 1000m, new DateTime(2026, 1, 15)),
            new("C002", "Global Tech", 2000m, new DateTime(2026, 1, 16))
        };
        
        var analyzer = CreateAnalyzer();
        
        // Act
        var result = analyzer.GetTopCustomers(transactions, 10);
        
        // Assert
        result.Should().HaveCount(2);
    }
    
    [Fact]
    public void CalculateTotalRevenue_SumsAllTransactions()
    {
        // Arrange
        var transactions = new List<SaleTransaction>
        {
            new("C001", "Acme Corp", 1000m, new DateTime(2026, 1, 15)),
            new("C002", "Global Tech", 2000m, new DateTime(2026, 1, 16)),
            new("C003", "Innovation Ltd", 1500m, new DateTime(2026, 1, 17))
        };
        
        var analyzer = CreateAnalyzer();
        
        // Act
        var result = analyzer.CalculateTotalRevenue(transactions);
        
        // Assert
        result.Should().Be(4500m);
    }
    
    [Fact]
    public void CalculateTotalRevenue_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        
        // Act
        var result = analyzer.CalculateTotalRevenue(new List<SaleTransaction>());
        
        // Assert
        result.Should().Be(0m);
    }
    
    private static ISalesAnalyzer CreateAnalyzer()
    {
        // TODO: Return your implementation here
        return new SalesAnalyzer();
        throw new NotImplementedException("Create SalesAnalyzer class that implements ISalesAnalyzer");
    }
}
