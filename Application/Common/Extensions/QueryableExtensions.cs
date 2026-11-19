using System.Linq.Expressions;

namespace HrApi.Application.Common.Extensions;

// Application layer: generic query helpers that any use case/service can reuse.
// They know nothing about Employee or HR rules, so they don't belong in Domain,
// and the service uses them, so they can't live in the API layer.
public static class QueryableExtensions
{
    // IQueryable + Expression (not IEnumerable + Func) so a real database provider
    // like EF Core can translate the sort into SQL ORDER BY instead of sorting in memory.
    public static IQueryable<T> SortBy<T, TKey>(
        this IQueryable<T> rows, bool? isAsc, Expression<Func<T, TKey>> keySelector) =>
        isAsc switch
        {
            true => rows.OrderBy(keySelector),
            false => rows.OrderByDescending(keySelector),
            null => rows
        };
}
