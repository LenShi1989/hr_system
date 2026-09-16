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

public record RoleUserCountDto(long Id, int Count);