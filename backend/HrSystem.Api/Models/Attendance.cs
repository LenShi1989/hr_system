namespace HrSystem.Api.Models;

public class LeaveType
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal? AnnualQuota { get; set; }
    public bool IsPaid { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<LeaveRequest> Requests { get; set; } = [];
}

public class LeaveRequest
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public long LeaveTypeId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public decimal Days { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public long? ApproverId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Employee? Employee { get; set; }
    public Employee? Approver { get; set; }
    public LeaveType? LeaveType { get; set; }
}

public class OvertimeRequest
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public decimal Hours { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public long? ApproverId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Employee? Employee { get; set; }
    public Employee? Approver { get; set; }
}

public class AttendanceRecord
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTimeOffset? ClockInAt { get; set; }
    public DateTimeOffset? ClockOutAt { get; set; }
    public decimal WorkHours { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public string Status { get; set; } = "normal";
    public Employee? Employee { get; set; }
}