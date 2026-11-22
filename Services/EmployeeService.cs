using System.Collections.Immutable;
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

    public Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken ct);
}

// Fake "database" in memory. Registered as a SINGLETON, so the same list lives for the
// whole app and is shared by ALL requests at the same time -> must be thread-safe (lock).
public class InMemoryEmployeeService : IEmployeeService
{
    private readonly List<Department> _departmentsDb =
    [
        new() { Id = 1, Name = "Hr" },
        new() { Id = 2, Name = "Backend" },
        new() { Id = 3, Name = "Mobile" },
    ];

    private readonly List<Employee> _employeesDb =
    [
        new()
        {
            Id = 1, Name = "Ali", Email = "ali@corp.com", DepartmentId = 3, Salary = 15_000m,
            HiredOn = new(2023, 3, 1), PasswordHash = "Admin@123"
        },
        new()
        {
            Id = 2, Name = "Sara", Email = "sara@corp.com", DepartmentId = 1, Salary = 12_000m,
            HiredOn = new(2022, 7, 15), PasswordHash = "Admin@123"
        },
        new()
        {
            Id = 3, Name = "Omar", Email = "omar@corp.com", DepartmentId = 2, Salary = 18_000m,
            HiredOn = new(2021, 1, 10), PasswordHash = "Admin@123"
        },
        new()
        {
            Id = 4, Name = "Salma", Email = "salma@corp.com", DepartmentId = 3, Salary = 16_000m,
            HiredOn = new(2024, 9, 5), PasswordHash = "Admin@123"
        },
    ];

    private int _nextId = 5;
    private readonly Lock _gate = new();

    public Task<PagedResponse<EmployeeResponse>> GetAllAsync(
        EmployeeQuery q, CancellationToken ct)
    {
        lock (_gate)
        {
            var filtered = _employeesDb.AsQueryable().ApplyFilters(q);
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
        lock (_gate) return Task.FromResult(_employeesDb.FirstOrDefault(e => e.Id == id)?.ToResponse());
    }

    public Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest r, CancellationToken ct)
    {
        lock (_gate)
        {
            var entity = new Employee
            {
                Id = _nextId++, Name = r.Name, Email = r.Email, DepartmentId = r.DepartmentId,
                Salary = r.Salary, HiredOn = DateOnly.FromDateTime(DateTime.Today),
                PasswordHash = Guid.NewGuid().ToString(),
            };
            _employeesDb.Add(entity);
            return Task.FromResult(entity.ToResponse());
        }
    }

    public Task<EmployeeResponse?> UpdateAsync(int id, UpdateEmployeeRequest r, CancellationToken ct)
    {
        lock (_gate)
        {
            var e = _employeesDb.FirstOrDefault(x => x.Id == id);
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
        lock (_gate) return Task.FromResult(_employeesDb.RemoveAll(e => e.Id == id) > 0);
    }

    // BUSINESS RULE: lives here, not in an attribute. [Range(1, 100)] on the DTO only checks
    // the SHAPE of the input ("is it a sane percent?"). "Max 50%" is a company policy: it can
    // change, depend on role or data, and must hold for every caller (controller, minimal API, tests).
    public const int MaxRaisePercent = 50;

    public Task<PromoteResult> PromoteAsync(int id, PromoteRequest r, CancellationToken ct)
    {
        lock (_gate)
        {
            var e = _employeesDb.FirstOrDefault(x => x.Id == id);
            if (e is null)
                return Task.FromResult(PromoteResult.Fail(PromoteStatus.EmployeeNotFound,
                    $"Employee with id {id} does not exist."));

            if (r.RaisePercent > MaxRaisePercent)
                return Task.FromResult(PromoteResult.Fail(PromoteStatus.RaiseTooHigh,
                    $"A promotion raise can be at most {MaxRaisePercent}%. You asked for {r.RaisePercent}%."));

            if (r.NewDepartmentId is { } depId && _departmentsDb.All(d => d.Id != depId))
                return Task.FromResult(PromoteResult.Fail(PromoteStatus.DepartmentNotFound,
                    $"Department with id {depId} does not exist."));

            e.Salary = Math.Round(e.Salary * (1 + r.RaisePercent / 100m), 2);
            if (r.NewDepartmentId is { } newDep) e.DepartmentId = newDep;
            return Task.FromResult(PromoteResult.Ok(e.ToResponse()));
        }
    }
    

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        lock (_gate)
            return Task.FromResult(_employeesDb.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken ct) =>
        Task.FromResult(_departmentsDb.Any(d => d.Id == departmentId));
}