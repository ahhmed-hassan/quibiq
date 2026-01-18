using System.Globalization;

namespace LinqPractice.Core;

/// <summary>
/// PROBLEM 2: System Integration - Data Transformation ⭐⭐ (30 minutes)
/// 
/// Real-World Context (from your CV!):
/// You integrated JIRA and TRAC systems - this is similar!
/// Transform data between SAP (German format) and Salesforce (English format).
/// 
/// Your Task:
/// Implement TransformOrders() that converts SAP orders to Salesforce format.
/// 
/// Data Transformations Required:
/// 1. Parse German decimal: "1.234,56" → 1234.56
/// 2. Parse German date: "31.12.2024" → DateTime(2024, 12, 31)
/// 3. Count total items: Flatten Positionen and count
/// 4. Handle missing/invalid data gracefully (skip invalid orders)
/// 
/// Expected LINQ Operations:
/// - Select (projection/transformation)
/// - SelectMany (flatten nested Positionen)
/// - Where (filter invalid data)
/// - Consider using .TryParse() methods
/// 
/// Example Input:
/// SapOrder {
///   BestellNummer: "ORD-2024-001",
///   KundenId: "K12345",
///   Betrag: "1.234,56",    // German decimal!
///   Datum: "31.12.2024",    // German date!
///   Positionen: [{ ... }, { ... }]  // 2 items
/// }
/// 
/// Expected Output:
/// SalesforceOrder {
///   OrderNumber: "ORD-2024-001",
///   CustomerId: "K12345",
///   Amount: 1234.56M,
///   OrderDate: DateTime(2024, 12, 31),
///   ItemCount: 2
/// }
/// 
/// Edge Cases to Handle:
/// - Betrag is null/empty → Skip order
/// - Betrag has invalid format → Skip order
/// - Datum is null/empty → Skip order
/// - Datum has invalid format → Skip order
/// - Positionen is null → ItemCount = 0
/// </summary>
public class SystemIntegrationTransformer
{
    public List<SalesforceOrder> TransformOrders(List<SapOrder> sapOrders)
    {
            
        return sapOrders
            .Select(sapOrder =>
        {
            decimal? amount = ParseGermanDecimal(sapOrder.Betrag);
            if (!amount.HasValue) return null;
            DateTime? date = ParseGermanDate(sapOrder.Datum);
            if (!date.HasValue) return null;
            return
            new SalesforceOrder
            {
                OrderNumber = sapOrder.BestellNummer,
                Amount = amount.Value, 
                CustomerId = sapOrder.KundenId,
                ItemCount = sapOrder.Positionen.Count,
                OrderDate = date.Value,
            };
        }
        )
            .OfType<SalesforceOrder>()
            .ToList();
        
    }
    
    /// <summary>
    /// Helper: Parse German decimal format to decimal
    /// Example: "1.234,56" → 1234.56M
    /// </summary>
    private decimal? ParseGermanDecimal(string value)
    {
     
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Remove thousand separators, replace decimal comma with dot (as it is the standrard for decimal)
        var normalized = value.Replace(".", "").Replace(",", ".");

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;


    }
    
    /// <summary>
    /// Helper: Parse German date format to DateTime
    /// Example: "31.12.2024" → DateTime(2024, 12, 31)
    /// </summary>
    private DateTime? ParseGermanDate(string value)
    {
       
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParseExact(
            value,
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var result))
        {
            return result;
        }

        return null;

    }
}
