using HrApi.Dtos;
using HrApi.Models;

namespace HrApi.Services;

// ISP (Interface Segregation): EmployeesController only needs to ask "does department X exist?".
// It depends on this tiny interface, not on the whole department CRUD.
public interface IDepartmentLookup
{
    Task<bool> ExistsAsync(int departmentId, CancellationToken ct);
}

// The full department use cases. DepartmentsController depends on this INTERFACE (DIP),
// Program.cs decides the implementation (in-memory today, EF Core later).
public interface IDepartmentService : IDepartmentLookup
{
    Task<IReadOnlyList<DepartmentResponse>> GetAllAsync(CancellationToken ct);
    Task<DepartmentResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<DepartmentResult> CreateAsync(CreateDepartmentRequest request, CancellationToken ct);
    Task<DepartmentResult> UpdateAsync(int id, UpdateDepartmentRequest request, CancellationToken ct);
    Task<DepartmentResult> DeleteAsync(int id, CancellationToken ct);
}

// SRP: this class owns the department RULES only. The data lives in HrDataStore.
public class InMemoryDepartmentService(HrDataStore store) : IDepartmentService
{
    public Task<IReadOnlyList<DepartmentResponse>> GetAllAsync(CancellationToken ct)
    {
        lock (store.Gate)
        {
            var counts = store.Employees.CountByDepartment();
            IReadOnlyList<DepartmentResponse> items = store.Departments
                .OrderBy(d => d.Id)
                .Select(d => d.ToResponse(counts.GetValueOrDefault(d.Id)))
                .ToList();
            return Task.FromResult(items);
        }
    }

    public Task<DepartmentResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        lock (store.Gate)
            return Task.FromResult(store.Departments.FindById(id)?.ToResponse(store.Employees.CountInDepartment(id)));
    }

    public Task<bool> ExistsAsync(int departmentId, CancellationToken ct)
    {
        lock (store.Gate)
            return Task.FromResult(store.Departments.FindById(departmentId) is not null);
    }

    public Task<DepartmentResult> CreateAsync(CreateDepartmentRequest r, CancellationToken ct)
    {
        lock (store.Gate)
        {
            if (store.Departments.IsNameTaken(r.Name))
                return Task.FromResult(NameTaken(r.Name));

            var entity = new Department { Id = store.NextDepartmentId(), Name = r.Name.NormalizeName() };
            store.Departments.Add(entity);
            return Task.FromResult(DepartmentResult.Ok(entity.ToResponse(employeeCount: 0)));
        }
    }

    public Task<DepartmentResult> UpdateAsync(int id, UpdateDepartmentRequest r, CancellationToken ct)
    {
        lock (store.Gate)
        {
            var d = store.Departments.FindById(id);
            if (d is null) return Task.FromResult(NotFound(id));

            if (store.Departments.IsNameTaken(r.Name, exceptId: id))
                return Task.FromResult(NameTaken(r.Name));

            d.Name = r.Name.NormalizeName();
            return Task.FromResult(DepartmentResult.Ok(d.ToResponse(store.Employees.CountInDepartment(id))));
        }
    }

    // BUSINESS RULE: a department with employees can't be deleted, or those employees
    // would point to a department that doesn't exist (in SQL: a foreign key violation).
    public Task<DepartmentResult> DeleteAsync(int id, CancellationToken ct)
    {
        lock (store.Gate)
        {
            var d = store.Departments.FindById(id);
            if (d is null) return Task.FromResult(NotFound(id));

            var employeeCount = store.Employees.CountInDepartment(id);
            if (employeeCount > 0)
                return Task.FromResult(DepartmentResult.Fail(DepartmentStatus.HasEmployees,
                    $"Department '{d.Name}' still has {employeeCount} employee(s). Move them first."));

            store.Departments.Remove(d);
            return Task.FromResult(DepartmentResult.Ok());
        }
    }

    private static DepartmentResult NotFound(int id) =>
        DepartmentResult.Fail(DepartmentStatus.NotFound, $"Department with id {id} does not exist.");

    private static DepartmentResult NameTaken(string name) =>
        DepartmentResult.Fail(DepartmentStatus.NameTaken, $"A department named '{name.NormalizeName()}' already exists.");
}
