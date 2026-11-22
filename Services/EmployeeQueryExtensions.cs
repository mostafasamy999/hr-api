using System.Linq.Expressions;
using HrApi.Application.Common.Extensions;
using HrApi.Dtos;
using HrApi.Models;

namespace HrApi.Services;

// Each step of the "list employees" pipeline is one small, named method.
// Any IEmployeeService (in-memory today, EF Core later) can reuse them.
public static class EmployeeQueryExtensions
{
    public static IQueryable<Employee> ApplyFilters(this IQueryable<Employee> rows, EmployeeQuery q)
    {
        Expression<Func<Employee, bool>> departmentCondition = e =>
            q.DepartmentId == null ||
            e.DepartmentId == q.DepartmentId;

        Expression<Func<Employee, bool>> searchCondition = e =>
            string.IsNullOrWhiteSpace(q.Search) ||
            e.Name.Contains(q.Search, StringComparison.OrdinalIgnoreCase);

        Expression<Func<Employee, bool>> salaryCondition = e =>
            q.MinSalary == null ||
            q.MinSalary <= 0 ||
            e.Salary >= q.MinSalary;

        return rows
            .Where(departmentCondition)
            .Where(searchCondition)
            .Where(salaryCondition);
    }

    public static IQueryable<Employee> ApplySort(this IQueryable<Employee> rows, EmployeeQuery q)
    {
        // Ascending unless the caller asks for "desc".
        var isAsc = !string.Equals(q.sortType, "desc", StringComparison.OrdinalIgnoreCase);

        return q.sortedBy?.ToLowerInvariant() switch
        {
            "salary" => rows.SortBy(isAsc, e => e.Salary),
            "department" => rows.SortBy(isAsc, e => e.DepartmentId),
            "name" => rows.SortBy(isAsc, e => e.Name),
            _ => rows.OrderBy(e => e.Id) // stable default so pages never shuffle
        };
    }

    public static IQueryable<Employee> ApplyPaging(this IQueryable<Employee> rows, int page, int pageSize) =>
        rows.Skip((page - 1) * pageSize).Take(pageSize);
}
