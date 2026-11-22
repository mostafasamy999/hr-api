using HrApi.Dtos;

namespace HrApi.Services;

// A promotion can fail in more than one way, so returning null is not enough anymore.
// The SERVICE decides what went wrong (business rules); the controller/endpoint only
// turns each status into the right HTTP response.
public enum PromoteStatus
{
    Success,
    EmployeeNotFound,     // -> 404
    DepartmentNotFound,   // -> 400 (bad input, same as create/update)
    RaiseTooHigh,         // -> 422 (valid input, but breaks a business rule)
}

public record PromoteResult(PromoteStatus Status, EmployeeResponse? Employee = null, string? Error = null)
{
    public static PromoteResult Ok(EmployeeResponse employee) => new(PromoteStatus.Success, employee);
    public static PromoteResult Fail(PromoteStatus status, string error) => new(status, Error: error);
}
