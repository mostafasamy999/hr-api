using HrApi.Dtos;

namespace HrApi.Services;

// Same pattern as PromoteResult: create/update/delete can fail in more than one way,
// so the service returns a VALUE that says what happened (no exceptions for normal business cases).
// The controller only turns each status into an HTTP response.
public enum DepartmentStatus
{
    Success,
    NotFound,       // -> 404
    NameTaken,      // -> 409 (another department already has this name)
    HasEmployees,   // -> 409 (can't delete: the department still has people)
}

public record DepartmentResult(DepartmentStatus Status, DepartmentResponse? Department = null, string? Error = null)
{
    public static DepartmentResult Ok(DepartmentResponse? department = null) => new(DepartmentStatus.Success, department);
    public static DepartmentResult Fail(DepartmentStatus status, string error) => new(status, Error: error);
}
