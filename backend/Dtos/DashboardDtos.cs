namespace HrSystem.Api.Dtos;

public record CountItemDto(string Name, int Count);

public record EmployeeStatsDto(
    int Total,
    IReadOnlyList<CountItemDto> ByDepartment,
    IReadOnlyList<CountItemDto> ByPosition);

public record PayrollTrendItemDto(string Period, decimal NetPay);

public record PayrollStatusItemDto(string Status, int Count);

public record PayrollSummaryDto(
    decimal CurrentMonthNetPay,
    IReadOnlyList<PayrollTrendItemDto> Trend,
    IReadOnlyList<PayrollStatusItemDto> ByStatus);

public record AttendanceSummaryDto(
    int TodayClockedIn,
    int TodayClockedOut,
    int ThisMonthWorkDays);

public record PendingApprovalsDto(int Leave, int Overtime);

public record MyDashboardDto(
    string TodayStatus,
    int MonthWorkDays,
    decimal MonthWorkHours,
    decimal MonthOvertimeHours,
    int PendingLeave,
    int PendingOvertime);

public record DashboardSummaryDto(
    EmployeeStatsDto? Employees,
    PayrollSummaryDto? Payroll,
    AttendanceSummaryDto? Attendance,
    PendingApprovalsDto? Pending,
    MyDashboardDto? My);