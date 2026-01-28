using FluentAssertions;
using QuibiqDay7Practice.Problem3;
using Xunit;

namespace QuibiqDay7Practice.Tests;

public class Problem3_OrderTransformationTests
{
    // TODO: Implement OrderTransformationService class that implements IOrderTransformationService
    
    [Fact]
    public void TransformOrders_WithValidOrders_ReturnsTransformedOrders()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new("SAP001", "CUST123", "Acme Corporation", 1000m, "NEW"),
            new("SAP002", "CUST456", "Global Tech Ltd", 2000m, "CONFIRMED")
        };
        
        var service = CreateService();
        
        // Act
        var result = service.TransformOrders(sapOrders);
        
        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        
        var first = result.Value[0];
        first.OrderId.Should().Be("SAP001");
        first.CustomerId.Should().Be("CUST123");
        first.CustomerName.Should().Be("Acme Corporation");
        first.Amount.Should().Be(1000m);
        first.Status.Should().Be(OrderStatus.Pending);
        
        var second = result.Value[1];
        second.Status.Should().Be(OrderStatus.Confirmed);
    }
    
    [Fact]
    public void TransformOrders_WithEmptyOrderNumber_ReturnsValidationError()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new("", "CUST123", "Acme Corp", 1000m, "NEW")
        };
        
        var service = CreateService();
        
        // Act
        var result = service.TransformOrders(sapOrders);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrderTransformation.InvalidOrderNumber");
    }
    
    [Fact]
    public void TransformOrders_WithEmptyCustomerCode_ReturnsValidationError()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new("SAP001", "", "Acme Corp", 1000m, "NEW")
        };
        
        var service = CreateService();
        
        // Act
        var result = service.TransformOrders(sapOrders);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrderTransformation.InvalidCustomerCode");
    }
    
    [Fact]
    public void TransformOrders_WithZeroAmount_ReturnsValidationError()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new("SAP001", "CUST123", "Acme Corp", 0m, "NEW")
        };
        
        var service = CreateService();
        
        // Act
        var result = service.TransformOrders(sapOrders);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrderTransformation.InvalidAmount");
    }
    
    [Fact]
    public void TransformOrders_WithNegativeAmount_ReturnsValidationError()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new("SAP001", "CUST123", "Acme Corp", -100m, "NEW")
        };
        
        var service = CreateService();
        
        // Act
        var result = service.TransformOrders(sapOrders);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrderTransformation.InvalidAmount");
    }
    
    [Fact]
    public void TransformOrders_WithInvalidStatus_ReturnsValidationError()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new("SAP001", "CUST123", "Acme Corp", 1000m, "INVALID_STATUS")
        };
        
        var service = CreateService();
        
        // Act
        var result = service.TransformOrders(sapOrders);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrderTransformation.InvalidStatus");
        result.FirstError.Description.Should().Contain("INVALID_STATUS");
    }
    
    [Fact]
    public void TransformOrders_ConvertsAllStatusValues_Correctly()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new("SAP001", "CUST123", "Acme Corp", 1000m, "PENDING"),
            new("SAP002", "CUST123", "Acme Corp", 1000m, "CONFIRMED"),
            new("SAP003", "CUST123", "Acme Corp", 1000m, "SHIPPED"),
            new("SAP004", "CUST123", "Acme Corp", 1000m, "DELIVERED")
        };
        
        var service = CreateService();
        
        // Act
        var result = service.TransformOrders(sapOrders);
        
        // Assert
        result.IsError.Should().BeFalse();
        result.Value[0].Status.Should().Be(OrderStatus.Pending);
        result.Value[1].Status.Should().Be(OrderStatus.Confirmed);
        result.Value[2].Status.Should().Be(OrderStatus.Shipped);
        result.Value[3].Status.Should().Be(OrderStatus.Delivered);
    }
    
    [Fact]
    public void TransformOrders_WithMixedValidAndInvalid_StopsAtFirstError()
    {
        // Arrange - first order valid, second invalid
        var sapOrders = new List<SapOrder>
        {
            new("SAP001", "CUST123", "Acme Corp", 1000m, "PENDING"),
            new("SAP002", "", "Global Tech", 2000m, "CONFIRMED"),  // Invalid
            new("SAP003", "CUST789", "Innovation Ltd", 1500m, "SHIPPED")
        };
        
        var service = CreateService();
        
        // Act
        var result = service.TransformOrders(sapOrders);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrderTransformation.InvalidCustomerCode");
    }
    
    [Fact]
    public void ValidateOrder_WithValidOrder_ReturnsSuccess()
    {
        // Arrange
        var order = new SapOrder("SAP001", "CUST123", "Acme Corp", 1000m, "PENDING");
        var service = CreateService();
        
        // Act
        var result = service.ValidateOrder(order);
        
        // Assert
        result.IsError.Should().BeFalse();
    }
    
    [Fact]
    public void ValidateOrder_WithInvalidOrder_ReturnsError()
    {
        // Arrange
        var order = new SapOrder("", "CUST123", "Acme Corp", 1000m, "NEW");
        var service = CreateService();
        
        // Act
        var result = service.ValidateOrder(order);
        
        // Assert
        result.IsError.Should().BeTrue();
    }
    
    private static IOrderTransformationService CreateService()
    {
        // TODO: Return your implementation here
         return new OrderTransformationService();
        throw new NotImplementedException("Create OrderTransformationService class that implements IOrderTransformationService");
    }
}
