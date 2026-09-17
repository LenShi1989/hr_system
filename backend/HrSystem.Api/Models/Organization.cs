namespace HrSystem.Api.Models;

public class Department
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Department? Parent { get; set; }
    public ICollection<Department> Children { get; set; } = [];
    public ICollection<Employee> Employees { get; set; } = [];
}

public class Position
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public long? DepartmentId { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Department? Department { get; set; }
    public ICollection<Employee> Employees { get; set; } = [];
}

public class Employee
{
    public long Id { get; set; }
    public required string EmployeeNo { get; set; }
    public required string Name { get; set; }
    public int Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateOnly? HireDate { get; set; }
    public DateOnly? LeaveDate { get; set; }
    public required string EmploymentStatus { get; set; }
    public long DepartmentId { get; set; }
    public long PositionId { get; set; }
    public long? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Department? Department { get; set; }
    public Position? Position { get; set; }
    public Employee? Manager { get; set; }
}