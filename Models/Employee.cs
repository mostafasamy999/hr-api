namespace HrApi.Models;

// The ENTITY: how data is stored (in a real app, an EF Core table).
// Mutable class, and it has fields the client must never see (PasswordHash).
// It never leaves the server — controllers return DTOs instead.
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal Salary { get; set; }
    public DateOnly HiredOn { get; set; }
    public string PasswordHash { get; set; } = "";
}
