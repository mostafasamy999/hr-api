using System.ComponentModel.DataAnnotations;
using HrApi.Models;

namespace HrApi.Dtos;

// Same rule as employees: the entity (Models/Department.cs) never leaves the server.
// The client only sees these records.

// ---------- what the client SENDS ----------
public record CreateDepartmentRequest(
    [Required, StringLength(50, MinimumLength = 2)]
    string Name);

public record UpdateDepartmentRequest(
    [Required, StringLength(50, MinimumLength = 2)]
    string Name);

// ---------- what the API RETURNS ----------
// EmployeeCount is computed, not stored: it is always correct and never goes stale.
public record DepartmentResponse(int Id, string Name, int EmployeeCount);

// ---------- mapping entity -> DTO ----------
public static class DepartmentMappings
{
    public static DepartmentResponse ToResponse(this Department d, int employeeCount) =>
        new(d.Id, d.Name, employeeCount);
}
