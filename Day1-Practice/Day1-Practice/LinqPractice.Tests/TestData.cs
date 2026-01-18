namespace LinqPractice.Tests;

using LinqPractice.Core;

/// <summary>
/// Pre-populated test data for all LINQ exercises.
/// This simulates real-world data you might get from databases or APIs.
/// </summary>
public static class TestData
{
    // ========================================================================
    // PROBLEM 1: Customer Spending Analysis
    // ========================================================================
    
    public static List<Customer> GetCustomers()
    {
        return new List<Customer>
        {
            new() { CustomerId = 1, Name = "Alice Smith", Segment = "Premium" },
            new() { CustomerId = 2, Name = "Bob Johnson", Segment = "Premium" },
            new() { CustomerId = 3, Name = "Charlie Brown", Segment = "Standard" },
            new() { CustomerId = 4, Name = "Diana Prince", Segment = "Standard" },
            new() { CustomerId = 5, Name = "Eve Davis", Segment = "Basic" },
            new() { CustomerId = 6, Name = "Frank Miller", Segment = "Basic" },
            new() { CustomerId = 7, Name = "Grace Lee", Segment = "Premium" },
            new() { CustomerId = 8, Name = "Henry Wilson", Segment = "Standard" },
        };
    }
    
    public static List<Order> GetOrders()
    {
        return new List<Order>
        {
            // Alice (Premium) - 1500€ total → INCLUDED
            new() { OrderId = 1, CustomerId = 1, Amount = 800, Date = DateTime.Parse("2024-01-15") },
            new() { OrderId = 2, CustomerId = 1, Amount = 700, Date = DateTime.Parse("2024-02-20") },
            
            // Bob (Premium) - 2500€ total → INCLUDED
            new() { OrderId = 3, CustomerId = 2, Amount = 1500, Date = DateTime.Parse("2024-01-10") },
            new() { OrderId = 4, CustomerId = 2, Amount = 1000, Date = DateTime.Parse("2024-03-05") },
            
            // Charlie (Standard) - 1200€ total → INCLUDED
            new() { OrderId = 5, CustomerId = 3, Amount = 600, Date = DateTime.Parse("2024-02-01") },
            new() { OrderId = 6, CustomerId = 3, Amount = 600, Date = DateTime.Parse("2024-02-15") },
            
            // Diana (Standard) - 800€ total → EXCLUDED (< 1000)
            new() { OrderId = 7, CustomerId = 4, Amount = 400, Date = DateTime.Parse("2024-01-20") },
            new() { OrderId = 8, CustomerId = 4, Amount = 400, Date = DateTime.Parse("2024-03-10") },
            
            // Eve (Basic) - 500€ total → EXCLUDED (< 1000)
            new() { OrderId = 9, CustomerId = 5, Amount = 300, Date = DateTime.Parse("2024-01-25") },
            new() { OrderId = 10, CustomerId = 5, Amount = 200, Date = DateTime.Parse("2024-02-28") },
            
            // Frank (Basic) - 1100€ total → INCLUDED
            new() { OrderId = 11, CustomerId = 6, Amount = 600, Date = DateTime.Parse("2024-01-30") },
            new() { OrderId = 12, CustomerId = 6, Amount = 500, Date = DateTime.Parse("2024-03-15") },
            
            // Grace (Premium) - 3000€ total → INCLUDED
            new() { OrderId = 13, CustomerId = 7, Amount = 2000, Date = DateTime.Parse("2024-01-05") },
            new() { OrderId = 14, CustomerId = 7, Amount = 1000, Date = DateTime.Parse("2024-02-10") },
            
            // Henry (Standard) - 900€ total → EXCLUDED (< 1000)
            new() { OrderId = 15, CustomerId = 8, Amount = 450, Date = DateTime.Parse("2024-01-12") },
            new() { OrderId = 16, CustomerId = 8, Amount = 450, Date = DateTime.Parse("2024-03-20") },
        };
    }
    
    /// <summary>
    /// Expected result for Problem 1:
    /// Premium: 3 customers (Alice 1500, Bob 2500, Grace 3000) → Avg 2333.33, Total 7000
    /// Standard: 1 customer (Charlie 1200) → Avg 1200, Total 1200
    /// Basic: 1 customer (Frank 1100) → Avg 1100, Total 1100
    /// </summary>
    public static List<SpendingReport> GetExpectedSpendingReports()
    {
        return new List<SpendingReport>
        {
            new() { Segment = "Premium", CustomerCount = 3,  TotalRevenue = 7000M },
            new() { Segment = "Standard", CustomerCount = 1,  TotalRevenue = 1200M },
            new() { Segment = "Basic", CustomerCount = 1,  TotalRevenue = 1100M },
        };
    }
    
    // ========================================================================
    // PROBLEM 2: System Integration - Data Transformation
    // ========================================================================
    
    public static List<SapOrder> GetSapOrders()
    {
        return new List<SapOrder>
        {
            // Valid order 1
            new()
            {
                BestellNummer = "ORD-2024-001",
                KundenId = "K12345",
                Betrag = "1.234,56",          // German decimal
                Datum = "31.12.2024",          // German date
                Positionen = new List<SapPosition>
                {
                    new() { ArtikelNr = "ART-001", Menge = "2" },
                    new() { ArtikelNr = "ART-002", Menge = "3" }
                }
            },
            
            // Valid order 2 (no decimal places)
            new()
            {
                BestellNummer = "ORD-2024-002",
                KundenId = "K67890",
                Betrag = "500",
                Datum = "01.01.2024",
                Positionen = new List<SapPosition>
                {
                    new() { ArtikelNr = "ART-003", Menge = "1" }
                }
            },
            
            // Valid order 3 (large amount)
            new()
            {
                BestellNummer = "ORD-2024-003",
                KundenId = "K11111",
                Betrag = "12.345,67",          // Thousand separator
                Datum = "15.06.2024",
                Positionen = new List<SapPosition>()  // No items
            },
            
            // INVALID: Missing amount
            new()
            {
                BestellNummer = "ORD-2024-004",
                KundenId = "K22222",
                Betrag = "",                   // Empty!
                Datum = "20.03.2024",
                Positionen = new List<SapPosition>()
            },
            
            // INVALID: Invalid date format
            new()
            {
                BestellNummer = "ORD-2024-005",
                KundenId = "K33333",
                Betrag = "789,00",
                Datum = "2024-03-20",          // Wrong format!
                Positionen = new List<SapPosition>()
            },
            
            // INVALID: Invalid amount format
            new()
            {
                BestellNummer = "ORD-2024-006",
                KundenId = "K44444",
                Betrag = "invalid",
                Datum = "25.04.2024",
                Positionen = new List<SapPosition>()
            },
            
            // Valid order 4 (comma decimal)
            new()
            {
                BestellNummer = "ORD-2024-007",
                KundenId = "K55555",
                Betrag = "999,99",
                Datum = "30.12.2024",
                Positionen = new List<SapPosition>
                {
                    new() { ArtikelNr = "ART-004", Menge = "5" }
                }
            },
        };
    }
    
    public static List<SalesforceOrder> GetExpectedSalesforceOrders()
    {
        return new List<SalesforceOrder>
        {
            new()
            {
                OrderNumber = "ORD-2024-001",
                CustomerId = "K12345",
                Amount = 1234.56M,
                OrderDate = new DateTime(2024, 12, 31),
                ItemCount = 2
            },
            new()
            {
                OrderNumber = "ORD-2024-002",
                CustomerId = "K67890",
                Amount = 500M,
                OrderDate = new DateTime(2024, 1, 1),
                ItemCount = 1
            },
            new()
            {
                OrderNumber = "ORD-2024-003",
                CustomerId = "K11111",
                Amount = 12345.67M,
                OrderDate = new DateTime(2024, 6, 15),
                ItemCount = 0
            },
            new()
            {
                OrderNumber = "ORD-2024-007",
                CustomerId = "K55555",
                Amount = 999.99M,
                OrderDate = new DateTime(2024, 12, 30),
                ItemCount = 1
            },
        };
    }
    
    // ========================================================================
    // PROBLEM 3: Query Optimization
    // ========================================================================
    
    public static (List<Order>, List<Customer>) GetLargeDataset()
    {
        const int customersTotalNumber = 10_000;
        const int orderTotalNumber = 50_000;
        var customers = Enumerable.Range(1, customersTotalNumber)
            .Select(i => new Customer
            {
                CustomerId = i,
                Name = $"Customer {i}",
                Segment = (i % 3 )switch
                {
                    0 => "Premium",
                    1 => "Standard",
                    _ => "Basic"
                }
            })
            .ToList();
        
        var random = new Random(42); // Fixed seed for reproducible tests
        var orders = Enumerable.Range(1, orderTotalNumber)
            .Select(i => new Order
            {
                OrderId = i,
                CustomerId = random.Next(1, customersTotalNumber + 1), // Random customer
                Amount = random.Next(10, 1000),
                Date = DateTime.Now.AddDays(-random.Next(0, 365))
            })
            .ToList();
        
        return (orders, customers);
    }
}
