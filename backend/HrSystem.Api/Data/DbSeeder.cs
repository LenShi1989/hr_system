using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Data;

public class DbSeeder(HrDbContext db, PasswordHasher hasher, IReadOnlyDictionary<string, string> seedPasswords)
{
    public async Task SeedAsync()
    {
        var roles = new Dictionary<string, Role>();
        foreach (var code in PermissionCatalog.RoleDefaults.Keys)
        {
            var role = await db.Roles
                .Include(r => r.Permissions)
                .SingleOrDefaultAsync(r => r.Code == code);

            if (role is null)
            {
                role = new Role { Code = code, Name = RoleNames[code] };
                db.Roles.Add(role);
                await db.SaveChangesAsync();

                foreach (var pc in PermissionCatalog.RoleDefaults[code])
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = pc });
                }

                await db.SaveChangesAsync();
            }

            roles[code] = role;
        }

        foreach (var (code, password) in seedPasswords)
        {
            var email = $"{code}@hr.local";
            var exists = await db.Users.AnyAsync(u => u.Email == email);
            if (exists)
            {
                continue;
            }

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = hasher.Hash(password),
                RoleId = roles[code].Id,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();

        await SeedLeaveTypesAsync();
        await EnsureSeedEmployeesAsync();
        await SeedPayrollDataAsync();
        await SeedSidebarMenusAsync();
    }

    private async Task SeedLeaveTypesAsync()
    {
        var defaults = new (string Code, string Name, decimal? Quota, bool Paid)[]
        {
            ("annual", "特休", 7, true),
            ("sick", "病假", 30, true),
            ("personal", "事假", 7, false),
            ("marriage", "婚假", 8, true),
            ("maternity", "產假", null, true),
            ("maternity_p", "陪產假", 7, true)
        };

        var existing = await db.LeaveTypes.Select(t => t.Code).ToListAsync();
        foreach (var (code, name, quota, paid) in defaults.Where(d => !existing.Contains(d.Code)))
        {
            db.LeaveTypes.Add(new LeaveType { Code = code, Name = name, AnnualQuota = quota, IsPaid = paid });
        }

        await db.SaveChangesAsync();
    }

    private async Task EnsureSeedEmployeesAsync()
    {
        var users = await db.Users
            .Include(u => u.Role)
            .Where(u => u.Email.EndsWith("@hr.local"))
            .ToListAsync();

        var seedCodes = new[] { "admin", "hr", "manager", "employee" };

        var department = await db.Departments.FirstOrDefaultAsync(d => d.IsActive);
        if (department is null)
        {
            department = new Department { Code = "HQ", Name = "總公司" };
            db.Departments.Add(department);
            await db.SaveChangesAsync();
        }

        var position = await db.Positions.FirstOrDefaultAsync(p => p.IsActive);
        if (position is null)
        {
            position = new Position { Code = "SEED-POS", Name = "內勤" };
            db.Positions.Add(position);
            await db.SaveChangesAsync();
        }

        var seeded = new Dictionary<string, Employee>();
        foreach (var code in seedCodes)
        {
            var user = users.FirstOrDefault(u => u.Email == $"{code}@hr.local");
            if (user is null)
            {
                continue;
            }

            if (user.EmployeeId is long linkedId)
            {
                var linked = await db.Employees.FindAsync(linkedId);
                if (linked is not null)
                {
                    seeded[code] = linked;
                    continue;
                }
            }

            var employee = new Employee
            {
                EmployeeNo = code,
                Name = SeedEmployeeNames[code],
                Gender = 0,
                HireDate = DateOnly.FromDateTime(DateTime.Today),
                EmploymentStatus = "active",
                DepartmentId = department.Id,
                PositionId = position.Id
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            user.EmployeeId = employee.Id;
            await db.SaveChangesAsync();
            seeded[code] = employee;
        }

        if (seeded.TryGetValue("manager", out var managerEmp) &&
            seeded.TryGetValue("employee", out var employeeEmp) &&
            employeeEmp.ManagerId != managerEmp.Id)
        {
            employeeEmp.ManagerId = managerEmp.Id;
            await db.SaveChangesAsync();
        }
    }

    private static readonly IReadOnlyDictionary<string, string> SeedEmployeeNames =
        new Dictionary<string, string>
        {
            ["admin"] = "系統管理員",
            ["hr"] = "人事專員",
            ["manager"] = "部門主管",
            ["employee"] = "一般員工"
        };

    private async Task SeedPayrollDataAsync()
    {
        var settings = new (string Code, string Label, decimal Value)[]
        {
            (PayrollCalculator.InsuranceRate, "勞健保自付比例", 0.10m),
            (PayrollCalculator.TaxRate, "所得稅預扣率", 0.05m),
            (PayrollCalculator.OvertimeRate, "加班費時薪倍率", 1.5m)
        };

        var current = await db.PayrollSettings.Select(s => s.Code).ToListAsync();
        foreach (var (code, label, value) in settings.Where(s => !current.Contains(s.Code)))
        {
            db.PayrollSettings.Add(new PayrollSetting { Code = code, Label = label, Value = value });
        }

        var salaries = new (string EmployeeNo, decimal Base, decimal Position, decimal Meal)[]
        {
            ("admin", 80000m, 10000m, 2400m),
            ("hr", 50000m, 5000m, 2400m),
            ("manager", 65000m, 8000m, 2400m),
            ("employee", 38000m, 3000m, 2400m)
        };

        var today = DateOnly.FromDateTime(DateTime.Today);
        var employees = await db.Employees
            .Where(e => salaries.Select(s => s.EmployeeNo).Contains(e.EmployeeNo))
            .ToDictionaryAsync(e => e.EmployeeNo);

        foreach (var (employeeNo, baseSalary, positionAllowance, mealAllowance) in salaries)
        {
            if (!employees.TryGetValue(employeeNo, out var employee))
            {
                continue;
            }

            var hasSalary = await db.EmployeeSalaries.AnyAsync(s => s.EmployeeId == employee.Id);
            if (hasSalary)
            {
                continue;
            }

            db.EmployeeSalaries.Add(new EmployeeSalary
            {
                EmployeeId = employee.Id,
                BaseSalary = baseSalary,
                PositionAllowance = positionAllowance,
                MealAllowance = mealAllowance,
                EffectiveDate = today
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedSidebarMenusAsync()
    {
        var menus = new (string GroupTitle, int GroupOrder, string Label, string Route, string Icon, string Permission, int SortOrder)[]
        {
            ("", 0, "儀表板", "/", "📊", PermissionCatalog.DashboardRead, 0),

            ("組織員工", 1, "部門", "/organization/departments", "🏢", PermissionCatalog.EmployeeRead, 0),
            ("組織員工", 1, "職位", "/organization/positions", "🛠️", PermissionCatalog.EmployeeRead, 1),
            ("組織員工", 1, "員工", "/organization/employees", "👥", PermissionCatalog.EmployeeRead, 2),

            ("出勤請假", 2, "我的出勤", "/attendance/my", "🕘", PermissionCatalog.AttendanceSelf, 0),
            ("出勤請假", 2, "出勤記錄", "/attendance/records", "📅", PermissionCatalog.AttendanceRead, 1),
            ("出勤請假", 2, "我的請假", "/leave/my", "🏖️", PermissionCatalog.LeaveRequest, 2),
            ("出勤請假", 2, "請假審核", "/leave/review", "✅", PermissionCatalog.LeaveApprove, 3),
            ("出勤請假", 2, "我的加班", "/overtime/my", "🌙", PermissionCatalog.OvertimeRequest, 4),
            ("出勤請假", 2, "加班審核", "/overtime/review", "🚦", PermissionCatalog.OvertimeApprove, 5),

            ("薪資", 3, "薪資單", "/payroll/payrolls", "💵", PermissionCatalog.PayrollRead, 0),
            ("薪資", 3, "薪資結構", "/payroll/salaries", "⚙️", PermissionCatalog.PayrollManage, 1),

            ("系統", 4, "使用者", "/system/users", "👤", PermissionCatalog.UserManage, 0),
            ("系統", 4, "角色權限", "/system/roles", "🎫", PermissionCatalog.UserManage, 1),
            ("系統", 4, "操作紀錄", "/system/audit-logs", "📜", PermissionCatalog.AuditRead, 2)
        };

        if (await db.SidebarMenus.AnyAsync())
        {
            return;
        }

        foreach (var (groupTitle, groupOrder, label, route, icon, permission, sortOrder) in menus)
        {
            db.SidebarMenus.Add(new SidebarMenu
            {
                GroupTitle = groupTitle,
                GroupOrder = groupOrder,
                Label = label,
                Route = route,
                Icon = icon,
                PermissionCode = permission,
                SortOrder = sortOrder
            });
        }

        await db.SaveChangesAsync();
    }

    private static readonly IReadOnlyDictionary<string, string> RoleNames =
        new Dictionary<string, string>
        {
            ["admin"] = "系統管理員",
            ["hr"] = "人事",
            ["manager"] = "部門主管",
            ["employee"] = "一般員工"
        };
}