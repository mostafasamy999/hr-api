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
    Task<EmployeeResponse?> PromoteAsync(int id, PromoteRequest request, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
}

// Fake "database" in memory. Registered as a SINGLETON, so the same list lives for the
// whole app and is shared by ALL requests at the same time -> must be thread-safe (lock).
public class InMemoryEmployeeService : IEmployeeService
{
    private readonly List<Employee> _db =
    [
        new() { Id = 1, Name = "Ali",   Email = "ali@corp.com",   Department = "Mobile",  Salary = 15_000m, HiredOn = new(2023, 3, 1),  PasswordHash = "x" },
        new() { Id = 2, Name = "Sara",  Email = "sara@corp.com",  Department = "HR",      Salary = 12_000m, HiredOn = new(2022, 7, 15), PasswordHash = "x" },
        new() { Id = 3, Name = "Omar",  Email = "omar@corp.com",  Department = "Backend", Salary = 18_000m, HiredOn = new(2021, 1, 10), PasswordHash = "x" },
        new() { Id = 4, Name = "Salma", Email = "salma@corp.com", Department = "Mobile",  Salary = 16_000m, HiredOn = new(2024, 9, 5),  PasswordHash = "x" },
    ];
    private int _nextId = 5;
    private readonly Lock _gate = new();

    public Task<PagedResponse<EmployeeResponse>> GetAllAsync(EmployeeQuery q, CancellationToken ct)
    {
        lock (_gate)
        {
            IEnumerable<Employee> rows = _db;
            if (!string.IsNullOrWhiteSpace(q.Department))
                rows = rows.Where(e => e.Department.Equals(q.Department, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(q.Search))
                rows = rows.Where(e => e.Name.Contains(q.Search, StringComparison.OrdinalIgnoreCase));

            var filtered = rows.ToList();
            var page = filtered.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
                               .Select(e => e.ToResponse()).ToList();
            return Task.FromResult(new PagedResponse<EmployeeResponse>(page, q.Page, q.PageSize, filtered.Count));
        }
    }

    public Task<EmployeeResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        lock (_gate) return Task.FromResult(_db.FirstOrDefault(e => e.Id == id)?.ToResponse());
    }

    public Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest r, CancellationToken ct)
    {
        lock (_gate)
        {
            var entity = new Employee
            {
                Id = _nextId++, Name = r.Name, Email = r.Email, Department = r.Department,
                Salary = r.Salary, HiredOn = DateOnly.FromDateTime(DateTime.Today),
                PasswordHash = Guid.NewGuid().ToString(),
            };
            _db.Add(entity);
            return Task.FromResult(entity.ToResponse());
        }
    }

    public Task<EmployeeResponse?> UpdateAsync(int id, UpdateEmployeeRequest r, CancellationToken ct)
    {
        lock (_gate)
        {
            var e = _db.FirstOrDefault(x => x.Id == id);
            if (e is null) return Task.FromResult<EmployeeResponse?>(null);
            e.Name = r.Name;
            e.Number = r.Number; e.Department = r.Department; e.Salary = r.Salary;
            return Task.FromResult<EmployeeResponse?>(e.ToResponse());
        }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        lock (_gate) return Task.FromResult(_db.RemoveAll(e => e.Id == id) > 0);
    }

    public Task<EmployeeResponse?> PromoteAsync(int id, PromoteRequest r, CancellationToken ct)
    {
        lock (_gate)
        {
            var e = _db.FirstOrDefault(x => x.Id == id);
            if (e is null) return Task.FromResult<EmployeeResponse?>(null);
            e.Salary = Math.Round(e.Salary * (1 + r.RaisePercent / 100m), 2);
            if (!string.IsNullOrWhiteSpace(r.NewDepartment)) e.Department = r.NewDepartment;
            return Task.FromResult<EmployeeResponse?>(e.ToResponse());
        }
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        lock (_gate) return Task.FromResult(_db.Any(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }
}
