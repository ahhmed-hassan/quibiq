using FluentAssertions;
using QuibiqDay7Practice.Problem2;
using Xunit;

namespace QuibiqDay7Practice.Tests;

public class Problem2_ReconciliationTests
{
    // TODO: Implement DataReconciliationService class that implements IDataReconciliationService
    
    [Fact]
    public void ReconcileOrders_WithPerfectMatch_ReturnsAllInMatched()
    {
        // Arrange
        var erpOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15)),
            new("ORD002", 2000m, new DateTime(2026, 1, 16))
        };
        
        var crmOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15)),
            new("ORD002", 2000m, new DateTime(2026, 1, 16))
        };
        
        var service = CreateService();
        
        // Act
        var result = service.ReconcileOrders(erpOrders, crmOrders);
        
        // Assert
        result.Matched.Should().HaveCount(2);
        result.InErpOnly.Should().BeEmpty();
        result.InCrmOnly.Should().BeEmpty();
    }
    
    [Fact]
    public void ReconcileOrders_WithOrdersInErpOnly_IdentifiesMissingInCrm()
    {
        // Arrange
        var erpOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15)),
            new("ORD002", 2000m, new DateTime(2026, 1, 16)),
            new("ORD003", 1500m, new DateTime(2026, 1, 17))
        };
        
        var crmOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15))
        };
        
        var service = CreateService();
        
        // Act
        var result = service.ReconcileOrders(erpOrders, crmOrders);
        
        // Assert
        result.InErpOnly.Should().HaveCount(2);
        result.InErpOnly.Should().Contain(o => o.OrderId == "ORD002");
        result.InErpOnly.Should().Contain(o => o.OrderId == "ORD003");
        result.Matched.Should().HaveCount(1);
        result.InCrmOnly.Should().BeEmpty();
    }
    
    [Fact]
    public void ReconcileOrders_WithOrdersInCrmOnly_IdentifiesMissingInErp()
    {
        // Arrange
        var erpOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15))
        };
        
        var crmOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15)),
            new("ORD004", 3000m, new DateTime(2026, 1, 18)),
            new("ORD005", 2500m, new DateTime(2026, 1, 19))
        };
        
        var service = CreateService();
        
        // Act
        var result = service.ReconcileOrders(erpOrders, crmOrders);
        
        // Assert
        result.InCrmOnly.Should().HaveCount(2);
        result.InCrmOnly.Should().Contain(o => o.OrderId == "ORD004");
        result.InCrmOnly.Should().Contain(o => o.OrderId == "ORD005");
        result.Matched.Should().HaveCount(1);
        result.InErpOnly.Should().BeEmpty();
    }
    
    [Fact]
    public void ReconcileOrders_WithComplexScenario_IdentifiesAllCategories()
    {
        // Arrange
        var erpOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15)),  // Matched
            new("ORD002", 2000m, new DateTime(2026, 1, 16)),  // ERP only
            new("ORD003", 1500m, new DateTime(2026, 1, 17))   // Matched
        };
        
        var crmOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15)),  // Matched
            new("ORD003", 1500m, new DateTime(2026, 1, 17)),  // Matched
            new("ORD004", 3000m, new DateTime(2026, 1, 18))   // CRM only
        };
        
        var service = CreateService();
        
        // Act
        var result = service.ReconcileOrders(erpOrders, crmOrders);
        
        // Assert
        result.Matched.Should().HaveCount(2);
        result.Matched.Should().Contain(m => m.ErpOrder.OrderId == "ORD001");
        result.Matched.Should().Contain(m => m.ErpOrder.OrderId == "ORD003");
        
        result.InErpOnly.Should().HaveCount(1);
        result.InErpOnly.Should().Contain(o => o.OrderId == "ORD002");
        
        result.InCrmOnly.Should().HaveCount(1);
        result.InCrmOnly.Should().Contain(o => o.OrderId == "ORD004");
    }
    
    [Fact]
    public void ReconcileOrders_WithEmptyLists_ReturnsEmptyResults()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var result = service.ReconcileOrders(
            new List<Order>(), 
            new List<Order>());
        
        // Assert
        result.Matched.Should().BeEmpty();
        result.InErpOnly.Should().BeEmpty();
        result.InCrmOnly.Should().BeEmpty();
    }
    
    [Fact]
    public void FindMissingOrders_ReturnsOrdersInSourceButNotInTarget()
    {
        // Arrange
        var sourceOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15)),
            new("ORD002", 2000m, new DateTime(2026, 1, 16)),
            new("ORD003", 1500m, new DateTime(2026, 1, 17))
        };
        
        var targetOrders = new List<Order>
        {
            new("ORD001", 1000m, new DateTime(2026, 1, 15))
        };
        
        var service = CreateService();
        
        // Act
        var result = service.FindMissingOrders(sourceOrders, targetOrders);
        
        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(o => o.OrderId == "ORD002");
        result.Should().Contain(o => o.OrderId == "ORD003");
    }
    
    private static IDataReconciliationService CreateService()
    {
        // TODO: Return your implementation here
         return new DataReconciliationService();
        //throw new NotImplementedException("Create DataReconciliationService class that implements IDataReconciliationService");
    }
}
