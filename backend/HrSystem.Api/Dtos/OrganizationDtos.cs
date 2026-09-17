namespace HrSystem.Api.Dtos;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record DepartmentDto(
    long Id,
    long? ParentId,
    string Code,
    string Name,
    long? ManagerId,
    bool IsActive,
    List<DepartmentDto> Children);

public record DepartmentUpsertRequest(string Code, string Name, long? ParentId, long? ManagerId);

public record PositionDto(
    long Id,
    string Code,
    string Name,
    long? DepartmentId,
    string? DepartmentName,
    int Level,
    bool IsActive);

public record PositionUpsertRequest(string Code, string Name, long? DepartmentId, int Level);

public record EmployeeDto(
    long Id,
    string EmployeeNo,
    string Name,
    int Gender,
    DateOnly? BirthDate,
    string? Phone,
    string? Email,
    string? Address,
    DateOnly? HireDate,
    DateOnly? LeaveDate,
    string EmploymentStatus,
    long DepartmentId,
    string DepartmentName,
    long PositionId,
    string PositionName,
    long? ManagerId,
    string? ManagerName,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record EmployeeUpsertRequest(
    string EmployeeNo,
    string Name,
    int Gender,
    DateOnly? BirthDate,
    string? Phone,
    string? Email,
    string? Address,
    DateOnly? HireDate,
    DateOnly? LeaveDate,
    string EmploymentStatus,
    long DepartmentId,
    long PositionId,
    long? ManagerId);