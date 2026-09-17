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

    public const string AuditRead = "audit.read";

    public record PermissionDefinition(string Code, string Label, string Group);

    public static readonly IReadOnlyList<PermissionDefinition> Definitions =
    [
        new(DashboardRead, "檢視儀表板", "儀表板"),

        new(EmployeeRead, "檢視組織與員工", "組織員工"),
        new(EmployeeManage, "維護組織與員工", "組織員工"),

        new(AttendanceSelf, "個人打卡與出勤", "出勤請假"),
        new(AttendanceRead, "檢視出勤記錄", "出勤請假"),
        new(LeaveRequest, "申請請假", "出勤請假"),
        new(LeaveApprove, "審核請假", "出勤請假"),
        new(OvertimeRequest, "申請加班", "出勤請假"),
        new(OvertimeApprove, "審核加班", "出勤請假"),

        new(PayrollRead, "檢視薪資", "薪資"),
        new(PayrollManage, "維護薪資與結構", "薪資"),

        new(UserManage, "管理使用者與角色", "系統"),
        new(AuditRead, "檢視操作紀錄", "系統"),
    ];

    public static readonly IReadOnlyDictionary<string, string[]> RoleDefaults =
        new Dictionary<string, string[]>
        {
            ["admin"] =
            [
                DashboardRead, EmployeeManage, EmployeeRead,
                AttendanceRead, AttendanceSelf,
                LeaveApprove, LeaveRequest, OvertimeApprove, OvertimeRequest,
                PayrollRead, PayrollManage,
                UserManage, AuditRead
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
        Definitions.Select(d => d.Code).ToArray();
}