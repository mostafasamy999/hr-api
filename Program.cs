using HrApi.Endpoints;
using HrApi.Filters;
using HrApi.Services;

// ============================================================
// Program.cs has two phases:
//   1. builder.Services... -> register SERVICES (Dependency Injection container)
//   2. app.Use... / app.Map... -> build the request PIPELINE (middleware, in order)
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. SERVICES ----------
builder.Services.AddControllers();             // finds every class that inherits ControllerBase
builder.Services.AddValidation();              // .NET 10: [Required], [Range]... validation for MINIMAL APIs
                                               // (controllers get it from [ApiController] instead)
builder.Services.AddOpenApi();                 // generates /openapi/v1.json describing the API
builder.Services.AddProblemDetails();          // standard JSON shape for errors

// DI lifetimes:
//   AddSingleton -> ONE instance for the whole app        (our in-memory "DB" must survive between requests)
//   AddScoped    -> one instance PER HTTP REQUEST          (typical for EF Core DbContext / real services)
//   AddTransient -> a NEW instance every time it's injected (small stateless helpers)
builder.Services.AddSingleton<IEmployeeService, InMemoryEmployeeService>();
builder.Services.AddScoped<TimingFilter>();    // needed for [ServiceFilter(typeof(TimingFilter))]

var app = builder.Build();

// ---------- 2. PIPELINE (order matters: each request flows top -> bottom) ----------
app.UseExceptionHandler();                     // unhandled exception -> 500 ProblemDetails (no stack trace leak)
app.UseStatusCodePages();                      // empty 404/405... -> ProblemDetails body

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                          // GET /openapi/v1.json
}

// app.UseHttpsRedirection();                  // enable when you run with the https profile
app.UseAuthorization();

app.MapControllers();                          // CONTROLLERS: connect [Route]/[HttpGet]... to URLs  -> /api/employees
app.MapEmployeeEndpoints();                    // MINIMAL API: Endpoints/EmployeeEndpoints.cs         -> /api/minimal/employees

// The smallest possible Minimal API endpoint — one line, no class at all:
app.MapGet("/ping", () => "pong");

app.Run();
