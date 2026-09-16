namespace HrSystem.Api.Models;

public class Role
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<User> Users { get; set; } = [];
    public ICollection<RolePermission> Permissions { get; set; } = [];
}