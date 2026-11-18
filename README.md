# HrApi — How to Use the APIs

A small HR (employees) API built with **ASP.NET Core / .NET 10**.
The same Employees API is written **twice** so you can compare the two styles:

| Style | Base URL | Code |
|---|---|---|
| Controllers | `/api/employees` | [Controllers/EmployeesController.cs](Controllers/EmployeesController.cs) |
| Minimal API | `/api/minimal/employees` | [Endpoints/EmployeeEndpoints.cs](Endpoints/EmployeeEndpoints.cs) |

Both use the same DTOs ([Dtos/EmployeeDtos.cs](Dtos/EmployeeDtos.cs)) and the same service ([Services/EmployeeService.cs](Services/EmployeeService.cs)).
Data is kept **in memory** (4 seeded employees), so every restart resets it.

---

## 1. Run it

```bash
git clone https://github.com/mostafasamy999/hr-api.git
cd hr-api
dotnet run
```

The API listens at **http://localhost:5206**. Press `Ctrl+C` to stop it.

---

## 2. Endpoints

Base URL: `http://localhost:5206`

### Controller API

| Method | URL |
|---|---|
| GET | `/api/employees` |
| GET | `/api/employees/{id}` |
| POST | `/api/employees` |
| PUT | `/api/employees/{id}` |
| DELETE | `/api/employees/{id}` |
| POST | `/api/employees/{id}/promote` |
| GET | `/api/employees/whoami` |
| GET | `/api/employees/boom` |

### Minimal API

| Method | URL |
|---|---|
| GET | `/api/minimal/employees` |
| GET | `/api/minimal/employees/{id}` |
| POST | `/api/minimal/employees` |
| PUT | `/api/minimal/employees/{id}` |
| DELETE | `/api/minimal/employees/{id}` |
| POST | `/api/minimal/employees/{id}/promote` |
| GET | `/api/minimal/employees/whoami` |
| GET | `/api/minimal/employees/boom` |
| GET | `/ping` |
