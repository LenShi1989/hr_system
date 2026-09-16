namespace HrSystem.Api.Models;

public class RolePermission
{
    public long RoleId { get; set; }
    public required string PermissionCode { get; set; }
    public Role? Role { get; set; }
}