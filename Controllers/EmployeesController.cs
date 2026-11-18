using HrApi.Dtos;
using HrApi.Filters;
using HrApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HrApi.Controllers;

// ============================================================
// CONTROLLER-BASED API
//   A controller = a CLASS that groups related endpoints. Each public method = one endpoint ("action").
//
//   [ApiController] turns on API behaviors:
//     - automatic 400 when the model is invalid (no `if (!ModelState.IsValid)` needed)
//     - [FromBody] is inferred for complex types, [FromRoute]/[FromQuery] for simple ones
//     - errors are returned as ProblemDetails JSON (RFC 7807)
//     - attribute routing is required
//
//   [Route("api/[controller]")] -> [controller] = class name minus "Controller" -> /api/employees
//
//   ControllerBase (not Controller): Controller adds View() support for MVC web pages; APIs don't need it.
// ============================================================
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ServiceFilter(typeof(TimingFilter))]              // runs before/after EVERY action in this class
public class EmployeesController(IEmployeeService service, ILogger<EmployeesController> logger) : ControllerBase
{
    // ↑ primary constructor (C# 12): DI injects IEmployeeService and ILogger automatically.
    //   A NEW controller instance is created for EVERY request.

    // ------------------------------------------------------------
    // GET /api/employees?department=Mobile&search=a&page=1&pageSize=2
    // ------------------------------------------------------------
    // [FromQuery] on a complex type -> binds each property from the query string.
    // ActionResult<T> = "either a T (200 OK) or any other result like NotFound()".
    [HttpGet]
    [ProducesResponseType<PagedResponse<EmployeeResponse>>(StatusCodes.Status200OK)]   // documents OpenAPI
    public async Task<ActionResult<PagedResponse<EmployeeResponse>>> GetAll(
        [FromQuery] EmployeeQuery query, CancellationToken ct)   // ct: cancelled if the client disconnects
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return BadRequest("page must be >= 1 and pageSize between 1 and 100");

        return await service.GetAllAsync(query, ct);   // implicit conversion T -> 200 OK
    }

    // ------------------------------------------------------------
    // GET /api/employees/5
    // ------------------------------------------------------------
    // {id:int} is a ROUTE CONSTRAINT: /api/employees/abc doesn't match -> 404 (not a 400).
    // Name = "..." lets other actions build a URL to this one (used by CreatedAtAction/CreatedAtRoute).
    [HttpGet("{id:int}", Name = "GetEmployeeById")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetById(int id, CancellationToken ct)
    {
        var employee = await service.GetByIdAsync(id, ct);
        return employee is null ? NotFound() : Ok(employee);
    }

    // ------------------------------------------------------------
    // POST /api/employees      body: { "name": "...", "email": "...", ... }
    // ------------------------------------------------------------
    // The body is validated BEFORE this method runs (thanks to [ApiController] + attributes on the record).
    // 201 Created + a Location header pointing to the new resource = REST best practice.
    [HttpPost]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Create(CreateEmployeeRequest request, CancellationToken ct)
    {
        // Business rule that attributes can't check -> add your own error, return the same 400 shape
        if (await service.EmailExistsAsync(request.Email, ct))
            return Conflict(new ProblemDetails
            {
                Title = "Email already used",
                Detail = $"An employee with email '{request.Email}' already exists.",
                Status = StatusCodes.Status409Conflict,
            });

        var created = await service.CreateAsync(request, ct);
        logger.LogInformation("Created employee {Id}", created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ------------------------------------------------------------
    // PUT /api/employees/5     (replace the editable fields)
    // ------------------------------------------------------------
    [HttpPut("{id:int}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> Update(int id, UpdateEmployeeRequest request, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    // ------------------------------------------------------------
    // DELETE /api/employees/5
    // ------------------------------------------------------------
    // No body to return -> IActionResult (non-generic) + 204 No Content.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await service.DeleteAsync(id, ct) ? NoContent() : NotFound();

    // ------------------------------------------------------------
    // POST /api/employees/5/promote     (a custom ACTION, not plain CRUD)
    // ------------------------------------------------------------
    [HttpPost("{id:int}/promote")]
    public async Task<ActionResult<EmployeeResponse>> Promote(int id, PromoteRequest request, CancellationToken ct)
    {
        var promoted = await service.PromoteAsync(id, request, ct);
        return promoted is null ? NotFound() : Ok(promoted);
    }

    // ------------------------------------------------------------
    // GET /api/employees/whoami      (all the binding sources in one place)
    // ------------------------------------------------------------
    //   [FromRoute]    /api/employees/{id}
    //   [FromQuery]    ?lang=ar
    //   [FromBody]     JSON body (only ONE per action)
    //   [FromHeader]   X-Client-Version: 1.2
    //   [FromForm]     form fields / file uploads
    //   [FromServices] pull a service from DI just for this action
    [HttpGet("whoami")]
    public IActionResult WhoAmI(
        [FromHeader(Name = "X-Client-Version")] string? clientVersion,
        [FromQuery] string lang = "en",
        [FromServices] IWebHostEnvironment env = null!)
    {
        // Inside a controller you also have: HttpContext, Request, Response, User (claims), ModelState...
        return Ok(new
        {
            clientVersion = clientVersion ?? "(not sent)",
            lang,
            environment = env.EnvironmentName,
            path = Request.Path.Value,
            userAgent = Request.Headers.UserAgent.ToString(),
        });
    }

    // ------------------------------------------------------------
    // GET /api/employees/boom   -> unhandled exception -> global handler -> 500 ProblemDetails
    // ------------------------------------------------------------
    [HttpGet("boom")]
    [ApiExplorerSettings(IgnoreApi = true)]   // hide from OpenAPI docs
    public IActionResult Boom() => throw new InvalidOperationException("Payroll database is down");
}
