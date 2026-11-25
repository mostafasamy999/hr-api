using HrApi.Dtos;
using HrApi.Filters;
using HrApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HrApi.Controllers;

// /api/departments — full CRUD for departments.
// The controller is only the HTTP layer: no business rules here.
// It calls the service and maps each DepartmentStatus to an HTTP status code.
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ServiceFilter(typeof(TimingFilter))]
public class DepartmentsController(IDepartmentService service, ILogger<DepartmentsController> logger) : ControllerBase
{
    // GET /api/departments
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DepartmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentResponse>>> GetAll(CancellationToken ct) =>
        Ok(await service.GetAllAsync(ct));

    // GET /api/departments/2
    [HttpGet("{id:int}")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentResponse>> GetById(int id, CancellationToken ct) =>
        await service.GetByIdAsync(id, ct) is { } department ? Ok(department) : NotFound();

    // POST /api/departments      body: { "name": "Design" }
    [HttpPost]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartmentResponse>> Create(CreateDepartmentRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        if (result.Status != DepartmentStatus.Success) return ToError(result);

        logger.LogInformation("Created department {Id}", result.Department!.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Department.Id }, result.Department);
    }

    // PUT /api/departments/2     body: { "name": "Platform" }
    [HttpPut("{id:int}")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartmentResponse>> Update(int id, UpdateDepartmentRequest request,
        CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result.Status == DepartmentStatus.Success ? Ok(result.Department) : ToError(result);
    }

    // DELETE /api/departments/4
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await service.DeleteAsync(id, ct);
        return result.Status == DepartmentStatus.Success ? NoContent() : ToError(result);
    }

    // One place that knows "status -> HTTP code" (no copy-paste in every action).
    // 409 Conflict = the request is valid, but it conflicts with the current state of the data.
    private ObjectResult ToError(DepartmentResult result) => result.Status switch
    {
        DepartmentStatus.NotFound => Problem(title: "Department not found", detail: result.Error,
            statusCode: StatusCodes.Status404NotFound),
        DepartmentStatus.NameTaken => Problem(title: "Department name already used", detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        DepartmentStatus.HasEmployees => Problem(title: "Department is not empty", detail: result.Error,
            statusCode: StatusCodes.Status409Conflict),
        _ => throw new InvalidOperationException($"Unexpected status {result.Status}"),
    };
}
