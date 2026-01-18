namespace LinqPractice.Core;

// ============================================================================
// PROBLEM 1: Customer Spending Analysis Models
// ============================================================================

public class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty; // "Premium", "Standard", "Basic"
}

public class SpendingReport
{
    public string Segment { get; set; } = string.Empty;
    public int CustomerCount { get; set; } = 0 ; 
   // public decimal AverageSpending { get; set; }
   public decimal AverageSpending => TotalRevenue / CustomerCount;
    public decimal TotalRevenue { get; set; } = 0; 
}

// ============================================================================
// PROBLEM 2: System Integration - Data Transformation Models
// ============================================================================

// Source System (SAP - German format)
public class SapOrder
{
    public string BestellNummer { get; set; } = string.Empty;  // Order number
    public string KundenId { get; set; } = string.Empty;        // Customer ID
    public string Betrag { get; set; } = string.Empty;          // Amount (German format: "1.234,56")
    public string Datum { get; set; } = string.Empty;           // Date (German format: "31.12.2024")
    public List<SapPosition> Positionen { get; set; } = new();  // Order items
}

public class SapPosition
{
    public string ArtikelNr { get; set; } = string.Empty;       // Product number
    public string Menge { get; set; } = string.Empty;           // Quantity
}

// Target System (Salesforce - English format)
public class SalesforceOrder
{
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public int ItemCount { get; set; }
}

// ============================================================================
// PROBLEM 3: Query Optimization Models
// ============================================================================

public class OrderSummary
{
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
}

// ============================================================================
// BONUS PROBLEM: Dynamic Filtering Models
// ============================================================================

public class Appointment
{
    public int AppointmentId { get; set; }
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty; // "Scheduled", "Completed", "Cancelled"
}

public class AppointmentSearchCriteria
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? CustomerName { get; set; }
    public List<string> ServiceTypes { get; set; } = [];
    public decimal? MinAmount { get; set; }
    public string? Status { get; set; }
}
