namespace HrSystem.Api.Dtos;

public record UserDto(
    long Id,
    string Email,
    long? EmployeeId,
    string? EmployeeName,
    string? EmployeeNo,
    long RoleId,
    string RoleCode,
    string RoleName,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CreateUserRequest(string Email, string Password, long RoleId, long? EmployeeId = null, bool IsActive = true);

public record UpdateUserRequest(string Email, long RoleId, long? EmployeeId = null, bool IsActive = true, string? Password = null);

public record RoleDto(
    long Id,
    string Code,
    string Name,
    bool IsActive,
    int UserCount,
    string[] PermissionCodes);

public record PermissionDto(string Code, string Label, string Group);

public record CreateRoleRequest(string Code, string Name, string[] PermissionCodes);

public record UpdateRoleRequest(string Name, string[] PermissionCodes);