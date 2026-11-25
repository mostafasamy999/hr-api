using HrApi.Models;

namespace HrApi.Services;

// PURE functions (FP): the result depends only on the inputs, and they change nothing.
// No lock, no DI, no state -> trivial to unit test, and any IDepartmentService can reuse them.
public static class DepartmentQueryExtensions
{
    // " backend " and "Backend" are the same name.
    public static string NormalizeName(this string name) => name.Trim();

    public static Department? FindById(this IEnumerable<Department> departments, int id) =>
        departments.FirstOrDefault(d => d.Id == id);

    // exceptId: when renaming department 2, its own current name doesn't count as "taken".
    public static bool IsNameTaken(this IEnumerable<Department> departments, string name, int? exceptId = null) =>
        departments.Any(d => d.Id != exceptId &&
                             string.Equals(d.Name, name.NormalizeName(), StringComparison.OrdinalIgnoreCase));

    public static int CountInDepartment(this IEnumerable<Employee> employees, int departmentId) =>
        employees.Count(e => e.DepartmentId == departmentId);

    // Count everyone ONCE (O(n)) instead of once per department (O(n * m)).
    public static IReadOnlyDictionary<int, int> CountByDepartment(this IEnumerable<Employee> employees) =>
        employees.GroupBy(e => e.DepartmentId).ToDictionary(g => g.Key, g => g.Count());
}
