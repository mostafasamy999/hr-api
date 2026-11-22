using System.Diagnostics;
using HrApi.Dtos;
using HrApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HrApi.Endpoints;

// ============================================================
// MINIMAL API — the SAME Employees API as Controllers/EmployeesController.cs
//   Routes live under /api/minimal/employees so you can call both side by side.
//   Same DTOs, same IEmployeeService -> only the "HTTP layer" is different.
//
//   CONTROLLER                               MINIMAL API
//   ------------------------------------------------------------------------------
//   class : ControllerBase                   static class with MapXxx(...) calls
//   [Route("api/[controller]")]              app.MapGroup("/api/minimal/employees")
//   [HttpGet("{id:int}")]                    group.MapGet("/{id:int}", handler)
//   constructor injection                    services are handler PARAMETERS
//   ActionResult<T> + Ok()/NotFound()        Results<Ok<T>, NotFound> + TypedResults.Ok()
//   [FromQuery] EmployeeQuery q              [AsParameters] EmployeeQuery q
//   [ApiController] auto validation          builder.Services.AddValidation() (.NET 10)
//   Action filter (IAsyncActionFilter)       Endpoint filter (.AddEndpointFilter)
//   [ProducesResponseType] attributes        inferred from Results<...> return type
//   [ServiceFilter]/[Authorize] on class     .AddEndpointFilter()/.RequireAuthorization() on group
//   CreatedAtAction(nameof(GetById), ...)    TypedResults.CreatedAtRoute(..., "RouteName", ...)
// ============================================================
public static class EmployeeEndpoints
{
    // Extension method -> Program.cs just calls app.MapEmployeeEndpoints();
    // This is how you keep Program.cs small when you have many endpoints.
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        // MapGroup = shared prefix + shared settings for every endpoint inside (like a controller class)
        var group = app.MapGroup("/api/v2/employees")
            .WithTags("Employees (Minimal)")             // grouping in OpenAPI docs
            .AddEndpointFilter<TimingEndpointFilter>();  // runs around EVERY endpoint in the group

        // Handlers can be named static methods (below) — easier to read & unit test than big lambdas
        group.MapGet("/", GetAll).WithSummary("List employees with filter + paging");
        group.MapGet("/{id:int}", GetById).WithName(GetByIdRoute);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
        group.MapPost("/{id:int}/promote", Promote);
        group.MapGet("/count", GetCount);
        
        // Small endpoints can be inline lambdas. Parameters are bound by convention:
        //   simple type + name in route  -> route,   simple type otherwise -> query string
        //   registered service           -> DI,      complex type          -> JSON body
        //   HttpContext / HttpRequest / CancellationToken / ClaimsPrincipal -> special, injected directly
        group.MapGet("/whoami", (
                [FromHeader(Name = "X-Client-Version")] string? clientVersion,
                IWebHostEnvironment env,          // from DI, no attribute needed
                HttpRequest request,
                string lang = "en") =>            // from query string
            TypedResults.Ok(new
            {
                clientVersion = clientVersion ?? "(not sent)",
                lang,
                environment = env.EnvironmentName,
                path = request.Path.Value,
                userAgent = request.Headers.UserAgent.ToString(),
            }));

        group.MapGet("/boom", () => { throw new InvalidOperationException("Payroll database is down"); })
             .ExcludeFromDescription();             // hide from OpenAPI

        return app;
    }

    private const string GetByIdRoute = "Minimal_GetEmployeeById";   // route names must be unique app-wide

    // ------------------------------------------------------------
    // GET /api/minimal/employees?department=Mobile&page=1&pageSize=2
    // ------------------------------------------------------------
    // [AsParameters] = take the record's properties as individual query parameters.
    // Results<A, B> = "returns A OR B" -> the compiler checks it AND OpenAPI learns both responses.
    static async Task<Results<Ok<PagedResponse<EmployeeResponse>>, ValidationProblem>> GetAll(
        [AsParameters] EmployeeQuery query, IEmployeeService service, CancellationToken ct)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["paging"] = ["page must be >= 1 and pageSize between 1 and 100"],
            });

        return TypedResults.Ok(await service.GetAllAsync(query, ct));
    }

    static async Task<Results<Ok<CountResponse>, NotFound>> GetCount(
        IEmployeeService service, CancellationToken ct)
    {
        var query = new EmployeeQuery();
        var result = await service.GetAllAsync(query, ct);
        return TypedResults.Ok(new CountResponse(result.Total));
    }
        

    // ------------------------------------------------------------
    // GET /api/minimal/employees/5
    // ------------------------------------------------------------
    static async Task<Results<Ok<EmployeeResponse>, NotFound>> GetById(
        int id, IEmployeeService service, CancellationToken ct) =>
        await service.GetByIdAsync(id, ct) is { } employee
            ? TypedResults.Ok(employee)
            : TypedResults.NotFound();

    // ------------------------------------------------------------
    // POST /api/minimal/employees
    // ------------------------------------------------------------
    // The body is validated by AddValidation() BEFORE this runs (same attributes on the record).
    static async Task<Results<CreatedAtRoute<EmployeeResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateEmployeeRequest request, IEmployeeService service, ILoggerFactory loggers, CancellationToken ct)
    {
        if (await service.EmailExistsAsync(request.Email, ct))
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Email already used",
                Detail = $"An employee with email '{request.Email}' already exists.",
                Status = StatusCodes.Status409Conflict,
            });
        if (!await service.DepartmentExistsAsync(request.DepartmentId, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.DepartmentId)] = [$"Department with id {request.DepartmentId} does not exist."]
            });
        var created = await service.CreateAsync(request, ct);
        loggers.CreateLogger("EmployeeEndpoints").LogInformation("Created employee {Id}", created.Id);
        return TypedResults.CreatedAtRoute(created, GetByIdRoute, new { id = created.Id });
    }

    // ------------------------------------------------------------
    // PUT /api/minimal/employees/5
    // ------------------------------------------------------------
    static async Task<Results<Ok<EmployeeResponse>, NotFound>> Update(
        int id, UpdateEmployeeRequest request, IEmployeeService service, CancellationToken ct) =>
        await service.UpdateAsync(id, request, ct) is { } updated
            ? TypedResults.Ok(updated)
            : TypedResults.NotFound();

    // ------------------------------------------------------------
    // DELETE /api/minimal/employees/5
    // ------------------------------------------------------------
    static async Task<Results<NoContent, NotFound>> Delete(int id, IEmployeeService service, CancellationToken ct) =>
        await service.DeleteAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound();

    // ------------------------------------------------------------
    // POST /api/v2/employees/5/promote
    // ------------------------------------------------------------
    // Same service, same rules -> same status codes as the controller.
    static async Task<Results<Ok<EmployeeResponse>, NotFound, ValidationProblem, ProblemHttpResult>> Promote(
        int id, PromoteRequest request, IEmployeeService service, CancellationToken ct)
    {
        var result = await service.PromoteAsync(id, request, ct);
        return result.Status switch
        {
            PromoteStatus.Success => TypedResults.Ok(result.Employee!),
            PromoteStatus.EmployeeNotFound => TypedResults.NotFound(),
            PromoteStatus.DepartmentNotFound => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.NewDepartmentId)] = [result.Error!],
            }),
            _ => TypedResults.Problem(title: "Promotion not allowed", detail: result.Error,
                statusCode: StatusCodes.Status422UnprocessableEntity),
        };
    }
}

// ENDPOINT FILTER = the Minimal API version of an action filter.
// Runs before/after the handler; can read the handler's arguments or short-circuit.
public class TimingEndpointFilter(ILogger<TimingEndpointFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var sw = Stopwatch.StartNew();
        var result = await next(context);   // <- the handler runs here

        context.HttpContext.Response.Headers["X-Elapsed-Ms"] = sw.ElapsedMilliseconds.ToString();
        logger.LogInformation("⏱ {Method} {Path} took {Ms} ms",
            context.HttpContext.Request.Method, context.HttpContext.Request.Path, sw.ElapsedMilliseconds);
        return result;
    }
}
