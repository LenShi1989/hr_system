namespace HrSystem.Api.Models;

public class User
{
    public long Id { get; set; }
    public long? EmployeeId { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public long RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Role? Role { get; set; }
    public Employee? Employee { get; set; }
}