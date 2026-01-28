using ErrorOr;

namespace QuibiqDay7Practice.Problem3;

/// <summary>
/// Order from SAP ERP system
/// </summary>
public record SapOrder(
    string OrderNumber,
    string CustomerCode,
    string CustomerFullName,
    decimal TotalAmount,
    string Status);  // Values: "NEW", "CONFIRMED", "SHIPPED", "DELIVERED"

/// <summary>
/// Order in E-commerce system format
/// </summary>
public record EcommerceOrder(
    string OrderId,
    string CustomerId,
    string CustomerName,
    decimal Amount,
    OrderStatus Status);

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered
}

/// <summary>
/// Service to transform orders from SAP to E-commerce format
/// </summary>
public interface IOrderTransformationService
{
    /// <summary>
    /// Transforms SAP orders to E-commerce format with validation
    /// Returns error if any order fails validation
    /// </summary>
    ErrorOr<List<EcommerceOrder>> TransformOrders(List<SapOrder> sapOrders);
    
    /// <summary>
    /// Validates a single SAP order
    /// </summary>
    ErrorOr<Success> ValidateOrder(SapOrder order);
}

public  class OrderTransformationService : IOrderTransformationService
{
    public  ErrorOr<List<EcommerceOrder>> TransformOrders(List<SapOrder> sapOrders)
    {
        var transformOrder = (SapOrder saporder) =>
        ValidateOrder(saporder)
            .Then(_ =>
                new EcommerceOrder(saporder.OrderNumber,
                saporder.CustomerCode,
                saporder.CustomerFullName,
                saporder.TotalAmount,
                Enum.Parse<OrderStatus>(saporder.Status== "NEW" ? "PENDING" :saporder.Status, true)
                ));

        List<EcommerceOrder> result = []; 
        foreach(var sapOrder in sapOrders)
        {
            var transformed = transformOrder(sapOrder);
            if (transformed.IsError)
                return transformed.Errors;

            result.Add(transformed.Value); 
        }

        return result; 
    }

    public  ErrorOr<Success> ValidateOrder(SapOrder order)
    {
        if(string.IsNullOrWhiteSpace(order.OrderNumber))
            return OrderTransformationErrors.InvalidOrderNumber;

        if (!Enum.TryParse<OrderStatus>(order.Status == "NEW" ? "PENDING" : order.Status, true, out OrderStatus status))
            return OrderTransformationErrors.InvalidStatus(order.Status);

        if (order.TotalAmount <= 0)
            return OrderTransformationErrors.InvalidAmount;

        if(string.IsNullOrEmpty(order.CustomerCode))
            return OrderTransformationErrors.InvalidCustomerCode;

        return new Success(); 
    }
}

/// <summary>
/// Custom errors for order transformation
/// </summary>
public static class OrderTransformationErrors
{
    public static Error InvalidOrderNumber => Error.Validation(
        "OrderTransformation.InvalidOrderNumber",
        "Order number cannot be empty");
    
    public static Error InvalidCustomerCode => Error.Validation(
        "OrderTransformation.InvalidCustomerCode",
        "Customer code cannot be empty");
    
    public static Error InvalidAmount => Error.Validation(
        "OrderTransformation.InvalidAmount",
        "Order amount must be greater than zero");
    
    public static Error InvalidStatus(string status) => Error.Validation(
        "OrderTransformation.InvalidStatus",
        $"Invalid order status: '{status}'. Valid values are: NEW, CONFIRMED, SHIPPED, DELIVERED");
}
