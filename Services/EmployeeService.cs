using HrApi.Dtos;
using HrApi.Models;

namespace HrApi.Services;

// The controller talks to this INTERFACE, not to a concrete class.
// Program.cs decides which implementation to inject (Dependency Injection).
// Tomorrow you swap InMemoryEmployeeService for an EF Core one — the controller doesn't change.
public interface IEmployeeService
{
    Task<PagedResponse<EmployeeResponse>> GetAllAsync(EmployeeQuery query, CancellationToken ct);
    Task<EmployeeResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken ct);
    Task<EmployeeResponse?> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<PromoteResult> PromoteAsync(int id, PromoteRequest request, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    // "Does department X exist?" is NOT an employee question -> moved to IDepartmentLookup (SRP + ISP).
}

// Employee use cases. The data itself lives in HrDataStore (a singleton shared with departments),
// so this class keeps one job: the employee rules (SRP).
public class InMemoryEmployeeService(HrDataStore store) : IEmployeeService
{
    public Task<PagedResponse<EmployeeResponse>> GetAllAsync(
        EmployeeQuery q, CancellationToken ct)
    {
        lock (store.Gate)
        {
            var filtered = store.Employees.AsQueryable().ApplyFilters(q);
            var total = filtered.Count();

            var items = filtered
                .ApplySort(q)
                .ApplyPaging(q.Page, q.PageSize)
                .Select(e => e.ToResponse())
                .ToList();

            return Task.FromResult(new PagedResponse<EmployeeResponse>(total, items, q.Page, q.PageSize));
        }
    }

    public Task<EmployeeResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        lock (store.Gate) return Task.FromResult(store.Employees.FirstOrDefault(e => e.Id == id)?.ToResponse());
    }

    public Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest r, CancellationToken ct)
    {
        lock (store.Gate)
        {
            var entity = new Employee
            {
                Id = store.NextEmployeeId(), Name = r.Name, Email = r.Email, DepartmentId = r.DepartmentId,
                Salary = r.Salary, HiredOn = DateOnly.FromDateTime(DateTime.Today),
                PasswordHash = Guid.NewGuid().ToString(),
            };
            store.Employees.Add(entity);
            return Task.FromResult(entity.ToResponse());
        }
    }

    public Task<EmployeeResponse?> UpdateAsync(int id, UpdateEmployeeRequest r, CancellationToken ct)
    {
        lock (store.Gate)
        {
            var e = store.Employees.FirstOrDefault(x => x.Id == id);
            if (e is null) return Task.FromResult<EmployeeResponse?>(null);
            e.Name = r.Name;
            e.Number = r.Number;
            e.DepartmentId = r.DepartmentId;
            e.Salary = r.Salary;
            return Task.FromResult<EmployeeResponse?>(e.ToResponse());
        }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        lock (store.Gate) return Task.FromResult(store.Employees.RemoveAll(e => e.Id == id) > 0);
    }

    // BUSINESS RULE: lives here, not in an attribute. [Range(1, 100)] on the DTO only checks
    // the SHAPE of the input ("is it a sane percent?"). "Max 50%" is a company policy: it can
    // change, depend on role or data, and must hold for every caller (controller, minimal API, tests).
    public const int MaxRaisePercent = 50;

    public Task<PromoteResult> PromoteAsync(int id, PromoteRequest r, CancellationToken ct)
    {
        lock (store.Gate)
        {
            var e = store.Employees.FirstOrDefault(x => x.Id == id);
            if (e is null)
                return Task.FromResult(PromoteResult.Fail(PromoteStatus.EmployeeNotFound,
                    $"Employee with id {id} does not exist."));

            if (r.RaisePercent > MaxRaisePercent)
                return Task.FromResult(PromoteResult.Fail(PromoteStatus.RaiseTooHigh,
                    $"A promotion raise can be at most {MaxRaisePercent}%. You asked for {r.RaisePercent}%."));

            if (r.NewDepartmentId is { } depId && store.Departments.All(d => d.Id != depId))
                return Task.FromResult(PromoteResult.Fail(PromoteStatus.DepartmentNotFound,
                    $"Department with id {depId} does not exist."));

            e.Salary = Math.Round(e.Salary * (1 + r.RaisePercent / 100m), 2);
            if (r.NewDepartmentId is { } newDep) e.DepartmentId = newDep;
            return Task.FromResult(PromoteResult.Ok(e.ToResponse()));
        }
    }
    

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        lock (store.Gate)
            return Task.FromResult(store.Employees.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }

}