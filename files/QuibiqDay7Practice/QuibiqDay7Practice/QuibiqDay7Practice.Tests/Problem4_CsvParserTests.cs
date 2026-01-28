using FluentAssertions;
using QuibiqDay7Practice.Problem4;
using Xunit;

namespace QuibiqDay7Practice.Tests;

public class Problem4_CsvParserTests
{
    // TODO: Implement CsvInvoiceParser class that implements ICsvInvoiceParser
    
    [Fact]
    public void ParseInvoices_WithValidCsv_ReturnsInvoices()
    {
        // Arrange
        var csvLines = new List<string>
        {
            "INV001,CUST123,1500.50,2026-01-15",
            "INV002,CUST456,2000.00,2026-01-16",
            "INV003,CUST789,750.25,2026-01-17"
        };
        
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(csvLines);
        
        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(3);
        
        var first = result.Value[0];
        first.InvoiceId.Should().Be("INV001");
        first.CustomerId.Should().Be("CUST123");
        first.Amount.Should().Be(1500.50m);
        first.InvoiceDate.Should().Be(new DateTime(2026, 1, 15));
    }
    
    [Fact]
    public void ParseInvoices_WithInsufficientColumns_ReturnsError()
    {
        // Arrange
        var csvLines = new List<string>
        {
            "INV001,CUST123,1500.50"  // Missing date column
        };
        
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(csvLines);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CsvParser.InvalidFormat");
        result.FirstError.Description.Should().Contain("Line 1");
    }
    
    [Fact]
    public void ParseInvoices_WithEmptyInvoiceId_ReturnsError()
    {
        // Arrange
        var csvLines = new List<string>
        {
            ",CUST123,1500.50,2026-01-15"
        };
        
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(csvLines);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CsvParser.InvalidInvoiceId");
        result.FirstError.Description.Should().Contain("Line 1");
    }
    
    [Fact]
    public void ParseInvoices_WithEmptyCustomerId_ReturnsError()
    {
        // Arrange
        var csvLines = new List<string>
        {
            "INV001,,1500.50,2026-01-15"
        };
        
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(csvLines);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CsvParser.InvalidCustomerId");
    }
    
    [Fact]
    public void ParseInvoices_WithInvalidAmount_ReturnsError()
    {
        // Arrange
        var csvLines = new List<string>
        {
            "INV001,CUST123,invalid_amount,2026-01-15"
        };
        
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(csvLines);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CsvParser.InvalidAmount");
        result.FirstError.Description.Should().Contain("invalid_amount");
    }
    
    [Fact]
    public void ParseInvoices_WithInvalidDate_ReturnsError()
    {
        // Arrange
        var csvLines = new List<string>
        {
            "INV001,CUST123,1500.50,invalid-date"
        };
        
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(csvLines);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CsvParser.InvalidDate");
        result.FirstError.Description.Should().Contain("invalid-date");
    }
    
    [Fact]
    public void ParseInvoices_WithMultipleErrors_ReturnsFirstError()
    {
        // Arrange - first line valid, second line invalid
        var csvLines = new List<string>
        {
            "INV001,CUST123,1500.50,2026-01-15",
            "INV002,,2000.00,2026-01-16",  // Invalid customer ID
            "INV003,CUST789,invalid,2026-01-17"  // Invalid amount
        };
        
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(csvLines);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Description.Should().Contain("Line 2");
    }
    
    [Fact]
    public void ParseInvoices_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(new List<string>());
        
        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }
    
    [Fact]
    public void ParseInvoices_WithWhitespace_TrimsValues()
    {
        // Arrange
        var csvLines = new List<string>
        {
            " INV001 , CUST123 , 1500.50 , 2026-01-15 "
        };
        
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseInvoices(csvLines);
        
        // Assert
        result.IsError.Should().BeFalse();
        result.Value[0].InvoiceId.Should().Be("INV001");
        result.Value[0].CustomerId.Should().Be("CUST123");
    }
    
    [Fact]
    public void ParseSingleInvoice_WithValidLine_ReturnsInvoice()
    {
        // Arrange
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseSingleInvoice("INV001,CUST123,1500.50,2026-01-15", 1);
        
        // Assert
        result.IsError.Should().BeFalse();
        result.Value.InvoiceId.Should().Be("INV001");
    }
    
    [Fact]
    public void ParseSingleInvoice_WithInvalidLine_ReturnsError()
    {
        // Arrange
        var parser = CreateParser();
        
        // Act
        var result = parser.ParseSingleInvoice(",CUST123,1500.50,2026-01-15", 5);
        
        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Description.Should().Contain("Line 5");
    }
    
    private static ICsvInvoiceParser CreateParser()
    {
        // TODO: Return your implementation here
        return new CsvInvoiceParser();
        throw new NotImplementedException("Create CsvInvoiceParser class that implements ICsvInvoiceParser");
    }
}
