namespace LinqPractice.Tests;

using LinqPractice.Core;
using Xunit;

/// <summary>
/// Tests for Problem 2: System Integration - Data Transformation
/// 
/// Real-world scenario: Transforming data between SAP and Salesforce systems
/// Tests German format parsing and data validation
/// </summary>
public class Problem2_DataTransformation_Tests
{
    private readonly SystemIntegrationTransformer _transformer = new();
    
    [Fact]
    public void TransformOrders_WithValidOrders_ReturnsCorrectCount()
    {
        // Arrange
        var sapOrders = TestData.GetSapOrders();
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        // 7 total orders, but 3 are invalid → Should return 4 valid orders
        Assert.Equal(4, result.Count);
    }
    
    [Fact]
    public void TransformOrders_ParsesGermanDecimalCorrectly()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-001",
                KundenId = "K001",
                Betrag = "1.234,56",      // German: 1234.56
                Datum = "31.12.2024",
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Single(result);
        Assert.Equal(1234.56M, result[0].Amount);
    }
    
    [Fact]
    public void TransformOrders_ParsesGermanDecimalWithThousandSeparator()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-002",
                KundenId = "K002",
                Betrag = "12.345,67",     // German: 12345.67
                Datum = "15.06.2024",
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Single(result);
        Assert.Equal(12345.67M, result[0].Amount);
    }
    
    [Fact]
    public void TransformOrders_ParsesGermanDecimalWithoutCents()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-003",
                KundenId = "K003",
                Betrag = "500",           // No decimal places
                Datum = "01.01.2024",
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Single(result);
        Assert.Equal(500M, result[0].Amount);
    }
    
    [Fact]
    public void TransformOrders_ParsesGermanDateCorrectly()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-004",
                KundenId = "K004",
                Betrag = "100,00",
                Datum = "31.12.2024",     // DD.MM.YYYY
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Single(result);
        Assert.Equal(new DateTime(2024, 12, 31), result[0].OrderDate);
    }
    
    [Fact]
    public void TransformOrders_CountsItemsCorrectly()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-005",
                KundenId = "K005",
                Betrag = "100,00",
                Datum = "15.03.2024",
                Positionen = new List<SapPosition>
                {
                    new() { ArtikelNr = "ART-001", Menge = "2" },
                    new() { ArtikelNr = "ART-002", Menge = "3" },
                    new() { ArtikelNr = "ART-003", Menge = "1" }
                }
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Single(result);
        Assert.Equal(3, result[0].ItemCount);
    }
    
    [Fact]
    public void TransformOrders_HandlesOrderWithNoItems()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-006",
                KundenId = "K006",
                Betrag = "100,00",
                Datum = "20.05.2024",
                Positionen = new List<SapPosition>() // Empty list
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Single(result);
        Assert.Equal(0, result[0].ItemCount);
    }
    
    [Fact]
    public void TransformOrders_SkipsOrderWithEmptyAmount()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-INVALID-1",
                KundenId = "K007",
                Betrag = "",              // Invalid!
                Datum = "01.01.2024",
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Empty(result); // Should skip invalid order
    }
    
    [Fact]
    public void TransformOrders_SkipsOrderWithInvalidAmountFormat()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-INVALID-2",
                KundenId = "K008",
                Betrag = "invalid",       // Invalid!
                Datum = "01.01.2024",
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public void TransformOrders_SkipsOrderWithInvalidDateFormat()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "TEST-INVALID-3",
                KundenId = "K009",
                Betrag = "100,00",
                Datum = "2024-01-01",     // Wrong format! (Should be DD.MM.YYYY)
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public void TransformOrders_PreservesOrderNumber()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "ORD-2024-XYZ",
                KundenId = "K010",
                Betrag = "100,00",
                Datum = "01.01.2024",
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("ORD-2024-XYZ", result[0].OrderNumber);
    }
    
    [Fact]
    public void TransformOrders_PreservesCustomerId()
    {
        // Arrange
        var sapOrders = new List<SapOrder>
        {
            new()
            {
                BestellNummer = "ORD-001",
                KundenId = "CUST-12345",
                Betrag = "100,00",
                Datum = "01.01.2024",
                Positionen = new()
            }
        };
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Single(result);
        Assert.Equal("CUST-12345", result[0].CustomerId);
    }
    
    [Fact]
    public void TransformOrders_WithMixedValidAndInvalidOrders_ReturnsOnlyValid()
    {
        // Arrange
        var sapOrders = TestData.GetSapOrders(); // Contains both valid and invalid
        var expected = TestData.GetExpectedSalesforceOrders();
        
        // Act
        var result = _transformer.TransformOrders(sapOrders);
        
        // Assert
        Assert.Equal(expected.Count, result.Count);
        
        // Verify specific orders
        var order1 = result.FirstOrDefault(o => o.OrderNumber == "ORD-2024-001");
        Assert.NotNull(order1);
        Assert.Equal(1234.56M, order1.Amount);
        Assert.Equal(new DateTime(2024, 12, 31), order1.OrderDate);
        Assert.Equal(2, order1.ItemCount);
    }
}
