namespace HrSystem.Api.Models;

public static class PermissionCatalog
{
    public const string DashboardRead = "dashboard.read";

    public const string EmployeeManage = "employee.manage";
    public const string EmployeeRead = "employee.read";

    public const string AttendanceRead = "attendance.read";
    public const string AttendanceSelf = "attendance.self";

    public const string LeaveApprove = "leave.approve";
    public const string LeaveRequest = "leave.request";
    public const string OvertimeApprove = "overtime.approve";
    public const string OvertimeRequest = "overtime.request";

    public const string PayrollRead = "payroll.read";
    public const string PayrollManage = "payroll.manage";

    public const string UserManage = "user.manage";

    public static readonly IReadOnlyDictionary<string, string[]> RoleDefaults =
        new Dictionary<string, string[]>
        {
            ["admin"] =
            [
                DashboardRead, EmployeeManage, EmployeeRead,
                AttendanceRead, AttendanceSelf,
                LeaveApprove, LeaveRequest, OvertimeApprove, OvertimeRequest,
                PayrollRead, PayrollManage,
                UserManage
            ],
            ["hr"] =
            [
                DashboardRead, EmployeeManage, EmployeeRead,
                AttendanceRead, AttendanceSelf,
                LeaveApprove, LeaveRequest, OvertimeApprove, OvertimeRequest,
                PayrollRead, PayrollManage
            ],
            ["manager"] =
            [
                DashboardRead, EmployeeRead,
                AttendanceRead, AttendanceSelf,
                LeaveApprove, LeaveRequest, OvertimeApprove, OvertimeRequest,
                PayrollRead
            ],
            ["employee"] =
            [
                DashboardRead, AttendanceSelf, PayrollRead,
                LeaveRequest, OvertimeRequest
            ]
        };

    public static IReadOnlyList<string> AllCodes =>
        RoleDefaults.Values.SelectMany(v => v).Distinct().ToArray();
}