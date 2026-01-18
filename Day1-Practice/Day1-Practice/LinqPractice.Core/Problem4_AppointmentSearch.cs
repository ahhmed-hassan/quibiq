using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqPractice.Core;

internal class Problem4_AppointmentSearch
{
    public static List<Appointment> SearchAppointments(
    List<Appointment> appointments,
    AppointmentSearchCriteria criteria)
    {
        // Build dynamic query based on which criteria are set
        // - If FromDate is null, don't filter by date
        // - If CustomerName is empty, don't filter by name
        // - If ServiceTypes is empty, don't filter by service

        // YOUR IMPLEMENTATION
        // Hint: Chain Where() calls conditionally
        var result = appointments
            .AsQueryable()
            .Where(a => a.Status == criteria.Status)
            .Where(a=> criteria.ServiceTypes.Contains(a.ServiceType))
            .WhereIf(criteria.FromDate.HasValue, a => a.Date > criteria.FromDate!.Value)
            .WhereIf(criteria.ToDate.HasValue, a => a.Date < criteria.ToDate!.Value)
            .WhereIf(criteria.MinAmount.HasValue, a=> a.Amount> criteria.MinAmount!.Value)
            .WhereIf(!string.IsNullOrEmpty(criteria.CustomerName), a=> a.CustomerName == criteria.CustomerName)
            ;
        return result.ToList();
        ;
    }
}

file static class AppointmentQueryExtensions
{
    public static IQueryable<Appointment> WhereIf(
        this IQueryable<Appointment> appointments , 
        bool condition, 
        Expression<Func<Appointment,  bool>> predicate
        )
        =>
        condition ? appointments.Where(predicate) : appointments
        ;
       
}
