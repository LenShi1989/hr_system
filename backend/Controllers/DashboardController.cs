using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = PermissionCatalog.DashboardRead)]
public class DashboardController(HrDbContext db) : ApiControllerBase(db)
{
    private bool Has(string code) =>
        User.HasClaim(TokenService.PermissionClaimType, code);

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var myEmployeeId = await CurrentEmployeeIdAsync(ct);

        var summary = new DashboardSummaryDto(null, null, null, null, null);

        if (Has(PermissionCatalog.EmployeeRead))
        {
            summary = summary with { Employees = await BuildEmployeeStatsAsync(ct) };
        }

        if (Has(PermissionCatalog.PayrollRead))
        {
            summary = summary with { Payroll = await BuildPayrollSummaryAsync(myEmployeeId, ct) };
        }

        if (Has(PermissionCatalog.AttendanceRead))
        {
            summary = summary with { Attendance = await BuildAttendanceSummaryAsync(myEmployeeId, ct) };
        }

        summary = summary with { Pending = await BuildPendingAsync(myEmployeeId, ct) };

        if (myEmployeeId is long employeeId)
        {
            summary = summary with { My = await BuildMyAsync(employeeId, ct) };
        }

        return Ok(ApiResponse.Ok(summary));
    }

    private async Task<EmployeeStatsDto> BuildEmployeeStatsAsync(CancellationToken ct)
    {
        var rows = await db.Employees.AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => new
            {
                Department = e.Department != null ? e.Department.Name : "未設定",
                Position = e.Position != null ? e.Position.Name : "未設定"
            })
            .ToListAsync(ct);

        var byDepartment = rows
            .GroupBy(r => r.Department)
            .Select(g => new CountItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var byPosition = rows
            .GroupBy(r => r.Position)
            .Select(g => new CountItemDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new EmployeeStatsDto(rows.Count, byDepartment, byPosition);
    }

    private async Task<PayrollSummaryDto?> BuildPayrollSummaryAsync(long? myEmployeeId, CancellationToken ct)
    {
        var now = DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime.Date);

        var periods = new List<int>();
        for (var offset = 5; offset >= 0; offset--)
        {
            var d = now.AddMonths(-offset);
            periods.Add(d.Year * 100 + d.Month);
        }

        var query = db.Payrolls.AsNoTracking().Where(p => periods.Contains(p.Period));

        if (!IsHrAdmin)
        {
            var scope = await VisibleEmployeeIdsAsync(myEmployeeId, ct);
            if (scope is null)
            {
                return null;
            }

            query = query.Where(p => scope.Contains(p.EmployeeId));
        }

        var rows = await query
            .Select(p => new { p.Period, p.NetPay, p.Status })
            .ToListAsync(ct);

        var trend = periods
            .Select(period => new PayrollTrendItemDto(
                period.ToString(),
                rows.Where(r => r.Period == period).Sum(r => r.NetPay)))
            .ToList();

        var byStatus = rows
            .Where(r => r.Period == periods[^1])
            .GroupBy(r => r.Status)
            .Select(g => new PayrollStatusItemDto(g.Key, g.Count()))
            .OrderBy(x => x.Status)
            .ToList();

        return new PayrollSummaryDto(trend[^1].NetPay, trend, byStatus);
    }

    private async Task<AttendanceSummaryDto?> BuildAttendanceSummaryAsync(long? myEmployeeId, CancellationToken ct)
    {
        var now = DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime.Date);
        var monthStart = new DateOnly(now.Year, now.Month, 1);

        var query = db.AttendanceRecords.AsNoTracking();

        if (!IsHrAdmin)
        {
            var scope = await VisibleEmployeeIdsAsync(myEmployeeId, ct);
            if (scope is null)
            {
                return null;
            }

            query = query.Where(a => scope.Contains(a.EmployeeId));
        }

        var todayClockedIn = await query.CountAsync(a => a.WorkDate == now && a.ClockInAt != null, ct);
        var todayClockedOut = await query.CountAsync(a => a.WorkDate == now && a.ClockOutAt != null, ct);

        var monthWorkDays = await query
            .Where(a => a.WorkDate >= monthStart && a.WorkDate <= now)
            .Select(a => new { a.EmployeeId, a.WorkDate })
            .Distinct()
            .CountAsync(ct);

        return new AttendanceSummaryDto(todayClockedIn, todayClockedOut, monthWorkDays);
    }

    private async Task<PendingApprovalsDto?> BuildPendingAsync(long? myEmployeeId, CancellationToken ct)
    {
        var hasLeave = Has(PermissionCatalog.LeaveApprove);
        var hasOvertime = Has(PermissionCatalog.OvertimeApprove);
        if (!hasLeave && !hasOvertime)
        {
            return null;
        }

        IReadOnlyList<long>? scope = null;
        if (!IsHrAdmin)
        {
            scope = await VisibleEmployeeIdsAsync(myEmployeeId, ct);
        }

        var leaveCount = 0;
        if (hasLeave)
        {
            var q = db.LeaveRequests.AsNoTracking().Where(l => l.Status == "pending");
            if (scope is not null)
            {
                q = q.Where(l => scope.Contains(l.EmployeeId));
            }

            leaveCount = await q.CountAsync(ct);
        }

        var overtimeCount = 0;
        if (hasOvertime)
        {
            var q = db.OvertimeRequests.AsNoTracking().Where(o => o.Status == "pending");
            if (scope is not null)
            {
                q = q.Where(o => scope.Contains(o.EmployeeId));
            }

            overtimeCount = await q.CountAsync(ct);
        }

        return new PendingApprovalsDto(leaveCount, overtimeCount);
    }

    private async Task<MyDashboardDto> BuildMyAsync(long employeeId, CancellationToken ct)
    {
        var now = DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime.Date);
        var monthStart = new DateOnly(now.Year, now.Month, 1);

        var todayRecord = await db.AttendanceRecords.AsNoTracking()
            .SingleOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == now, ct);

        var todayStatus = todayRecord is null || todayRecord.ClockInAt is null
            ? "not_clocked"
            : todayRecord.ClockOutAt is null ? "clocked_in" : "clocked_out";

        var monthRecords = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate >= monthStart && a.WorkDate <= now)
            .Select(a => new { a.WorkDate, a.WorkHours })
            .ToListAsync(ct);

        var monthWorkDays = monthRecords.Select(r => r.WorkDate).Distinct().Count();
        var monthWorkHours = monthRecords.Sum(r => r.WorkHours);

        var monthOvertimeHours = await db.OvertimeRequests.AsNoTracking()
            .Where(o => o.EmployeeId == employeeId && o.Status == "approved"
                        && o.WorkDate >= monthStart && o.WorkDate <= now)
            .SumAsync(o => (decimal?)o.Hours, ct) ?? 0m;

        var pendingLeave = await db.LeaveRequests.AsNoTracking()
            .CountAsync(l => l.EmployeeId == employeeId && l.Status == "pending", ct);

        var pendingOvertime = await db.OvertimeRequests.AsNoTracking()
            .CountAsync(o => o.EmployeeId == employeeId && o.Status == "pending", ct);

        return new MyDashboardDto(
            todayStatus, monthWorkDays, monthWorkHours, monthOvertimeHours, pendingLeave, pendingOvertime);
    }

    private async Task<IReadOnlyList<long>?> VisibleEmployeeIdsAsync(long? myEmployeeId, CancellationToken ct)
    {
        if (myEmployeeId is null)
        {
            return null;
        }

        var managed = await db.Employees.AsNoTracking()
            .Where(e => e.ManagerId == myEmployeeId && e.IsActive)
            .Select(e => e.Id)
            .ToListAsync(ct);

        return managed.Append(myEmployeeId.Value).ToList();
    }
}