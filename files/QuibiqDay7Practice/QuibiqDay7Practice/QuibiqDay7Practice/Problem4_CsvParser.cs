using ErrorOr;

namespace QuibiqDay7Practice.Problem4;

/// <summary>
/// Represents a parsed invoice
/// </summary>
public record Invoice(
    string InvoiceId,
    string CustomerId,
    decimal Amount,
    DateTime InvoiceDate);

/// <summary>
/// Represents a parsing error with line number context
/// </summary>
public record ParsingError(int LineNumber, string Error);

/// <summary>
/// Service to parse CSV invoice data
/// </summary>
public interface ICsvInvoiceParser
{
    
    /// <summary>
    /// Parses CSV lines into Invoice objects
    /// CSV Format: InvoiceId,CustomerId,Amount,Date
    /// Returns error with all parsing errors if any line fails validation
    /// </summary>
    ErrorOr<List<Invoice>> ParseInvoices(List<string> csvLines);
    
    /// <summary>
    /// Validates a single invoice row
    /// </summary>
    ErrorOr<Invoice> ParseSingleInvoice(string csvLine, int lineNumber);
}

public class CsvInvoiceParser : ICsvInvoiceParser
{
    private const string CsvSep = ",";
    public ErrorOr<List<Invoice>> ParseInvoices(List<string> csvLines)
    {
        List<Invoice> res = [];
        foreach ((string csvLine, int index) in csvLines.Select((val, index) => (val,index+1)))
        {
            var parsedInvoice = ParseSingleInvoice(csvLine, index);
            if (parsedInvoice.IsError)
                return parsedInvoice.Errors;
            res.Add(parsedInvoice.Value);
        }
        return res; 
    }

    public ErrorOr<Invoice> ParseSingleInvoice(string csvLine, int lineNumber)
    {
        // Split without RemoveEmptyEntries - we need to detect empty fields!
        var parts = csvLine.Split(CsvSep, StringSplitOptions.TrimEntries);

        // Check column count
        if (parts.Length != 4)
            return CsvParsingErrors.InvalidFormat(lineNumber,
                $"Expected 4 columns, got {parts.Length}");

        var invoiceId = parts[0];
        var customerId = parts[1];
        var amountStr = parts[2];
        var dateStr = parts[3];

        if (string.IsNullOrWhiteSpace(invoiceId))
            return CsvParsingErrors.InvalidInvoiceId(lineNumber);

        if (string.IsNullOrWhiteSpace(customerId))
            return CsvParsingErrors.InvalidCustomerId(lineNumber);

        if (!decimal.TryParse(amountStr, out decimal amount))
            return CsvParsingErrors.InvalidAmount(lineNumber, amountStr);

        /* 
         * var formats = new[] { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy" };
         * DateTime.TryParse accepts different formatting options for making it more configurable
         */

        if (!DateTime.TryParse(dateStr, out DateTime date))
            return CsvParsingErrors.InvalidDate(lineNumber, dateStr);

        return new Invoice(invoiceId, customerId, amount, date);
    }
}

/// <summary>
/// Custom errors for CSV parsing
/// </summary>
file static class CsvParsingErrors
{
    public static Error InvalidFormat(int lineNumber, string reason) => Error.Validation(
        "CsvParser.InvalidFormat",
        $"Line {lineNumber}: {reason}");
    
    public static Error InvalidInvoiceId(int lineNumber) => Error.Validation(
        "CsvParser.InvalidInvoiceId",
        $"Line {lineNumber}: Invoice ID cannot be empty");
    
    public static Error InvalidCustomerId(int lineNumber) => Error.Validation(
        "CsvParser.InvalidCustomerId",
        $"Line {lineNumber}: Customer ID cannot be empty");
    
    public static Error InvalidAmount(int lineNumber, string value) => Error.Validation(
        "CsvParser.InvalidAmount",
        $"Line {lineNumber}: Invalid amount '{value}'");
    
    public static Error InvalidDate(int lineNumber, string value) => Error.Validation(
        "CsvParser.InvalidDate",
        $"Line {lineNumber}: Invalid date '{value}'");
}
