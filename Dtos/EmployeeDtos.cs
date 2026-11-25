using System.ComponentModel.DataAnnotations;
using HrApi.Models;

namespace HrApi.Dtos;

// DTOs = the API CONTRACT (what goes in and out over HTTP). Records are perfect for this.
// The validation attributes are checked AUTOMATICALLY because the controller has [ApiController]:
// an invalid body never reaches your action — the client gets a 400 ValidationProblemDetails.

// ---------- what the client SENDS ----------
public record CreateEmployeeRequest(
    [Required, StringLength(50, MinimumLength = 2)]
    string Name,
    [Required, EmailAddress] string Email,
    [Required]
    int DepartmentId,
    [Range(1_000, 1_000_000)] decimal Salary);

public record UpdateEmployeeRequest(
    [Required, StringLength(50, MinimumLength = 2)]
    string Name,
    string Number,
    int DepartmentId,
    [Range(1_000, 1_000_000)] decimal Salary);

// [Range(1, 100)] checks only the SHAPE (a sane percent). The "max 50%" business rule is in the service.
// NewDepartmentId is optional (null = stay in the same department); the service checks it exists.
public record PromoteRequest(
    [Range(1, 100)] int RaisePercent,
    int? NewDepartmentId);

// ---------- what the API RETURNS ----------
// No PasswordHash. Salary is included here, but you could make a separate "public" DTO without it.
public record EmployeeResponse(
    int Id,
    string Name,
    string Number,
    string Email,
    int DepartmentId,
    decimal Salary,
    DateOnly HiredOn);
public record CountResponse(int Count);

public record PagedResponse<T>(int Total, IReadOnlyList<T> Items, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

// ---------- query-string parameters grouped in one object ----------
// GET /api/employees?department=Mobile&search=al&page=1&pageSize=10
public record EmployeeQuery(
    int? DepartmentId = null,
    string? Search = null,
    int? MinSalary = null,
    string? sortedBy = null,
    string? sortType = null,
    int Page = 1,
    int PageSize = 10);

// ---------- mapping entity -> DTO ----------
public static class EmployeeMappings
{
    public static EmployeeResponse ToResponse(this Employee e) =>
        new(e.Id, e.Name, e.Number, e.Email, e.DepartmentId, e.Salary, e.HiredOn);
}