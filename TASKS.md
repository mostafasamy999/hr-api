# HrApi Learning Tasks

Do one task, run the API, check the result in `HrApi.http`, then tick it off and move on.
After each task, explain in one sentence which file you changed and why. If you can't, that's the part to ask about.

## Level 1 — Follow one request from URL to data

- [ ] **1. Add a field.** Add `Phone` to `Employee` in `Models/Employee.cs`, then make it show up in the API response.
  - Learn: entity vs DTO. You'll have to change `Dtos/EmployeeDtos.cs` (`EmployeeResponse`, `ToResponse`) too.
- [ ] **2. Prove the password stays hidden.** Call `GET /api/employees/1` and confirm `PasswordHash` isn't in the result. Add it to `EmployeeResponse` for a moment, see it leak, then remove it.
  - Learn: why DTOs exist.
- [ ] **3. Add `GET /api/employees/count`** that returns the total number of employees. Write it in the controller first, then in the Minimal API.
  - Learn: routing, `[HttpGet("count")]` vs `MapGet("/count")`, and the path controller → service → data.
- [ ] **4. Break the route on purpose.** Move `count` after `{id}`, remove `:int` from `{id:int}`, then call `/api/employees/count`.
  - Learn: route constraints and how routes are matched.

## Level 2 — Inputs and validation

- [ ] **1. Add a `minSalary` filter** to the list endpoint (`?minSalary=15000`).
  - Learn: `[FromQuery]` and `[AsParameters]` binding, and LINQ `Where`.
- [ ] **2. Add sorting:** `?sortBy=salary&desc=true`.
  - Learn: building a query step by step with `IEnumerable`.
- [ ] **3. Validate `Department`.** It must be `Mobile`, `HR`, `Backend` or `Design`. Do it with `[AllowedValues]` first, then with a custom check that returns 400.
  - Learn: automatic vs manual validation, and the ProblemDetails format.
- [ ] **4. Your own rule:** a promotion above 50% returns `422` with a clear error message.
  - Learn: business rules belong in the service or handler, not in attributes.

## Level 3 — The pipeline and DI

- [ ] **1. Request-logging middleware** in `Program.cs` with `app.Use(async (ctx, next) => …)` that logs method, path and status code. Move it above and below `UseExceptionHandler` and compare the output.
  - Learn: middleware order.
- [ ] **2. DI lifetimes.** Change `AddSingleton<IEmployeeService>` to `AddScoped`, create an employee, then list all. Where did it go? Change it back.
  - Learn: why the in-memory "database" must be a singleton.
- [ ] **3. Add an `IClock` service** with `Now`, inject it into the service, and use it for `HiredOn` on create.
  - Learn: constructor injection, and why depending on an interface makes testing easy.
- [ ] **4. API-key filter.** Reject any request without `X-Api-Key: secret` with 401. Write it once as a controller filter and once as an endpoint filter.
  - Learn: filters vs middleware, and short-circuiting a request.

## Level 4 — Make it real

- [ ] **1. Add Scalar or Swagger UI** to browse `/openapi/v1.json` visually.
- [ ] **2. Real database.** Add EF Core + SQLite, write `EfEmployeeService : IEmployeeService`, and change one line in `Program.cs`.
  - Learn: why the interface exists. Controller and endpoints shouldn't change.
- [ ] **3. Tests.** Create `HrApi.Tests` with `WebApplicationFactory`, one test per status code (200, 201, 400, 404, 409).
  - Learn: integration testing.
- [ ] **4. Second resource:** `Department` with full CRUD. Employees then link to a department id instead of a string.
  - Learn: designing a related resource from scratch.
