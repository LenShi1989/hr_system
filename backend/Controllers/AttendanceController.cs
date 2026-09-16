using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public class AttendanceController(HrDbContext db, WorkSchedule schedule) : ApiControllerBase(db)
{
    [HttpPost("me/clock-in")]
    [Authorize(Policy = PermissionCatalog.AttendanceSelf)]
    public async Task<IActionResult> ClockIn(CancellationToken ct)
    {
        var employeeId = await RequireLinkedEmployeeAsync(ct);
        if (employeeId is null)
        {
            return BadRequest(ApiResponse.Fail("no_employee_link", "目前帳號尚未綁定員工資料"));
        }

        var today = DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime.Date);
        var record = await db.AttendanceRecords
            .SingleOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today, ct);

        if (record is null)
        {
            record = new AttendanceRecord
            {
                EmployeeId = employeeId.Value,
                WorkDate = today,
                ClockInAt = DateTimeOffset.Now,
                Status = "normal"
            };
            db.AttendanceRecords.Add(record);
        }
        else if (record.ClockInAt is not null)
        {
            return BadRequest(ApiResponse.Fail("already_clocked_in", "今天已打過上班卡"));
        }
        else
        {
            record.ClockInAt = DateTimeOffset.Now;
        }

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok(ToDto(record, null)));
    }

    [HttpPost("me/clock-out")]
    [Authorize(Policy = PermissionCatalog.AttendanceSelf)]
    public async Task<IActionResult> ClockOut(CancellationToken ct)
    {
        var employeeId = await RequireLinkedEmployeeAsync(ct);
        if (employeeId is null)
        {
            return BadRequest(ApiResponse.Fail("no_employee_link", "目前帳號尚未綁定員工資料"));
        }

        var today = DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime.Date);
        var record = await db.AttendanceRecords
            .SingleOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today, ct);

        if (record is null || record.ClockInAt is null)
        {
            return BadRequest(ApiResponse.Fail("not_clocked_in", "尚未打卡上班，無法打下班卡"));
        }

        if (record.ClockOutAt is not null)
        {
            return BadRequest(ApiResponse.Fail("already_clocked_out", "今天已打過下班卡"));
        }

        var clockOut = DateTimeOffset.Now;
        record.ClockOutAt = clockOut;
        record.WorkHours = Math.Round((decimal)(clockOut - record.ClockInAt.Value).TotalHours, 2);

        var localIn = record.ClockInAt.Value.LocalDateTime;
        var localOut = clockOut.LocalDateTime;

        record.LateMinutes = localIn.TimeOfDay > schedule.Start
            ? (int)(localIn.TimeOfDay - schedule.Start).TotalMinutes
            : 0;
        record.EarlyLeaveMinutes = localOut.TimeOfDay < schedule.End
            ? (int)(schedule.End - localOut.TimeOfDay).TotalMinutes
            : 0;
        record.Status = record.LateMinutes > 0 ? "late" : record.EarlyLeaveMinutes > 0 ? "early_leave" : "normal";

        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok(ToDto(record, "下班打卡成功")));
    }

    [HttpGet("me")]
    [Authorize(Policy = PermissionCatalog.AttendanceSelf)]
    public async Task<IActionResult> MyRecords([FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var employeeId = await RequireLinkedEmployeeAsync(ct);
        if (employeeId is null)
        {
            return BadRequest(ApiResponse.Fail("no_employee_link", "目前帳號尚未綁定員工資料"));
        }

        return Ok(ApiResponse.Ok(await QueryRecordsAsync(new[] { employeeId.Value }, fromDate, toDate, ct)));
    }

    [HttpGet("records")]
    [Authorize(Policy = PermissionCatalog.AttendanceRead)]
    public async Task<IActionResult> Records(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] long? employeeId,
        CancellationToken ct)
    {
        var employeeIds = new List<long>();

        if (IsHrAdmin)
        {
            if (employeeId is long eid)
            {
                employeeIds.Add(eid);
            }
        }
        else
        {
            var myEmployeeId = await CurrentEmployeeIdAsync(ct);
            if (myEmployeeId is null)
            {
                return BadRequest(ApiResponse.Fail("no_employee_link", "目前帳號尚未綁定員工資料"));
            }

            var managed = await db.Employees
                .AsNoTracking()
                .Where(e => e.ManagerId == myEmployeeId && e.IsActive)
                .Select(e => e.Id)
                .ToListAsync(ct);

            employeeIds.AddRange(managed);
            employeeIds.Add(myEmployeeId.Value);
        }

        return Ok(ApiResponse.Ok(await QueryRecordsAsync(employeeIds, fromDate, toDate, ct)));
    }

    private async Task<List<AttendanceRecordDto>> QueryRecordsAsync(
        IReadOnlyList<long> employeeIds, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct)
    {
        var query = db.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => employeeIds.Contains(a.EmployeeId));

        if (fromDate is not null)
        {
            query = query.Where(a => a.WorkDate >= fromDate);
        }

        if (toDate is not null)
        {
            query = query.Where(a => a.WorkDate <= toDate);
        }

        return await query
            .OrderByDescending(a => a.WorkDate)
            .ThenBy(a => a.EmployeeId)
            .Select(a => new AttendanceRecordDto(
                a.Id, a.EmployeeId,
                a.Employee != null ? a.Employee.Name : null,
                a.Employee != null ? a.Employee.EmployeeNo : string.Empty,
                a.WorkDate, a.ClockInAt, a.ClockOutAt, a.WorkHours, a.LateMinutes, a.EarlyLeaveMinutes, a.Status))
            .ToListAsync(ct);
    }

    private async Task<long?> RequireLinkedEmployeeAsync(CancellationToken ct) =>
        await CurrentEmployeeIdAsync(ct);

    private static AttendanceRecordDto ToDto(AttendanceRecord a, string? employeeNo) =>
        new(
            a.Id, a.EmployeeId, null,
            employeeNo ?? string.Empty,
            a.WorkDate, a.ClockInAt, a.ClockOutAt,
            a.WorkHours, a.LateMinutes, a.EarlyLeaveMinutes, a.Status);
}