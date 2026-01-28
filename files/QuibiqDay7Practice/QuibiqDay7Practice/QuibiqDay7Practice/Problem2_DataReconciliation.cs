namespace QuibiqDay7Practice.Problem2;

/// <summary>
/// Represents an order in either ERP or CRM system
/// </summary>
public record Order(string OrderId, decimal Amount, DateTime OrderDate);

/// <summary>
/// Result of reconciling orders between two systems
/// </summary>
public record ReconciliationResult(
    List<Order> InErpOnly,      // Orders that exist in ERP but not in CRM
    List<Order> InCrmOnly,      // Orders that exist in CRM but not in ERP
    List<(Order ErpOrder, Order CrmOrder)> Matched  // Orders that exist in both systems
);

/// <summary>
/// Service to reconcile data between two systems
/// </summary>
public interface IDataReconciliationService
{
    /// <summary>
    /// Compares orders from ERP and CRM systems and identifies discrepancies
    /// </summary>
    ReconciliationResult ReconcileOrders(List<Order> erpOrders, List<Order> crmOrders);

    /// <summary>
    /// Finds orders that exist in source but not in target
    /// </summary>
    List<Order> FindMissingOrders(List<Order> sourceOrders, List<Order> targetOrders);
}
public class DataReconciliationService : IDataReconciliationService
{
    public List<Order> FindMissingOrders(List<Order> sourceOrders, List<Order> targetOrders)
    {
        return sourceOrders.ExceptBy(targetOrders.Select(targetOrder => targetOrder.OrderId),
            sourceOrder => sourceOrder.OrderId
            ).ToList();
    }

    public ReconciliationResult ReconcileOrders(List<Order> erpOrders, List<Order> crmOrders)
    {
        var erpOrdersLeftJoinCrmOrders = erpOrders
            .GroupJoin(crmOrders,
            erp => erp.OrderId,
            crm => crm.OrderId,
            (erp, crmOrders) => new
            {
                ErpOrder = erp,
                CrmOrders = crmOrders
            }
            );

        List<Order> erpOnlyOrders = erpOrdersLeftJoinCrmOrders
                .Where(x => !x.CrmOrders.Any())
                .Select(x => x.ErpOrder)
                .ToList();

        var matchedOrders = erpOrdersLeftJoinCrmOrders
            .Where(x => x.CrmOrders.Any())
            .SelectMany(x => x.CrmOrders.Select(crm => (x.ErpOrder, crm))
            )
            ;
        List<Order> crmOnlyOrders = crmOrders
            .ExceptBy(matchedOrders.Select(matched => matched.crm.OrderId),
            crm => crm.OrderId)
            .ToList();

        return new ReconciliationResult(
            erpOnlyOrders,
            crmOnlyOrders,
            matchedOrders.ToList()
            );
    }
}
