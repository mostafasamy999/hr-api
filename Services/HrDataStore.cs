using HrApi.Models;

namespace HrApi.Services;

// Fake "database" shared by ALL services (registered as a SINGLETON in Program.cs).
//
// Why a separate class?
//   - Employees need departments (does department 2 exist?) and departments need employees
//     (can I delete department 2, or does it still have people?).
//   - If EmployeeService depended on DepartmentService AND DepartmentService on EmployeeService,
//     DI would fail with a circular dependency.
//   - So the DATA lives here (one place, one lock) and each service keeps only its own LOGIC (SRP).
//   - In Level 4 this class is replaced by an EF Core DbContext — same idea.
public sealed class HrDataStore
{
    // One lock for every table: a request that reads employees + departments sees a consistent picture.
    public Lock Gate { get; } = new();

    public List<Department> Departments { get; } =
    [
        new() { Id = 1, Name = "Hr" },
        new() { Id = 2, Name = "Backend" },
        new() { Id = 3, Name = "Mobile" },
    ];

    public List<Employee> Employees { get; } =
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

    private int _nextEmployeeId = 5;
    private int _nextDepartmentId = 4;

    // Call these only while holding Gate.
    public int NextEmployeeId() => _nextEmployeeId++;
    public int NextDepartmentId() => _nextDepartmentId++;
}
