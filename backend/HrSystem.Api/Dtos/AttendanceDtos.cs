namespace HrSystem.Api.Dtos;

public record LeaveTypeDto(long Id, string Code, string Name, decimal? AnnualQuota, bool IsPaid, bool IsActive);

public record CreateLeaveRequest(long LeaveTypeId, DateTimeOffset StartAt, DateTimeOffset EndAt, string Reason);

public record LeaveRequestDto(
    long Id,
    long EmployeeId,
    string? EmployeeName,
    string EmployeeNo,
    long LeaveTypeId,
    string? LeaveTypeName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal Days,
    string Reason,
    string Status,
    long? ApproverId,
    string? ApproverName,
    DateTimeOffset CreatedAt);

public record CreateOvertimeRequest(DateOnly WorkDate, DateTimeOffset StartAt, DateTimeOffset EndAt, string Reason);

public record OvertimeRequestDto(
    long Id,
    long EmployeeId,
    string? EmployeeName,
    string EmployeeNo,
    DateOnly WorkDate,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal Hours,
    string Reason,
    string Status,
    long? ApproverId,
    string? ApproverName,
    DateTimeOffset CreatedAt);

public record ReviewRequest(string Action);

public record AttendanceRecordDto(
    long Id,
    long EmployeeId,
    string? EmployeeName,
    string EmployeeNo,
    DateOnly WorkDate,
    DateTimeOffset? ClockInAt,
    DateTimeOffset? ClockOutAt,
    decimal WorkHours,
    int LateMinutes,
    int EarlyLeaveMinutes,
    string Status);

public record ClockResultDto(AttendanceRecordDto Record, string Message);