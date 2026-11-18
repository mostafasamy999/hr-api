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
    [Required] string Department,
    [Range(1_000, 1_000_000)] decimal Salary);

public record UpdateEmployeeRequest(
    [Required, StringLength(50, MinimumLength = 2)]
    string Name,
    string Number,
    [Required] string Department,
    [Range(1_000, 1_000_000)] decimal Salary);

public record PromoteRequest([Range(1, 100)] int RaisePercent, string? NewDepartment);

// ---------- what the API RETURNS ----------
// No PasswordHash. Salary is included here, but you could make a separate "public" DTO without it.
public record EmployeeResponse(
    int Id,
    string Name,
    string Number,
    string Email,
    string Department,
    decimal Salary,
    DateOnly HiredOn);

public record CountResponse(int Count);

public record PagedResponse<T>(int Page, int PageSize, int Total, IReadOnlyList<T> Items)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

// ---------- query-string parameters grouped in one object ----------
// GET /api/employees?department=Mobile&search=al&page=1&pageSize=10
public record EmployeeQuery(
    string? Department = null,
    string? Search = null,
    int? MinSalary = null,
    int Page = 1,
    int PageSize = 10);

// ---------- mapping entity -> DTO ----------
public static class EmployeeMappings
{
    public static EmployeeResponse ToResponse(this Employee e) =>
        new(e.Id, e.Name, e.Number, e.Email, e.Department, e.Salary, e.HiredOn);
}