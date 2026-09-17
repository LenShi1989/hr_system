using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/overtime-requests")]
[Authorize]
public class OvertimeRequestsController(HrDbContext db, AuditLogService audit) : ApiControllerBase(db)
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] long? employeeId = null,
        [FromQuery] bool all = false,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var myEmployeeId = await CurrentEmployeeIdAsync(ct);
        long? targetEmployeeId = null;
        IReadOnlyList<long>? scopeEmployeeIds = null;

        if (employeeId is not null)
        {
            var scopeError = await EnsureCanViewOthersAsync(employeeId.Value, ct);
            if (scopeError is not null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("forbidden", scopeError));
            }

            targetEmployeeId = employeeId;
        }
        else if (all)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(ApiResponse.Fail("status_required", "使用 all 查詢時必須提供 status"));
            }

            if (!IsHrAdmin)
            {
                if (myEmployeeId is null)
                {
                    return BadRequest(ApiResponse.Fail("no_employee_link", "目前帳號尚未綁定員工資料"));
                }

                scopeEmployeeIds = await db.Employees
                    .AsNoTracking()
                    .Where(e => e.ManagerId == myEmployeeId && e.IsActive)
                    .Select(e => e.Id)
                    .ToListAsync(ct);
            }
        }
        else
        {
            if (myEmployeeId is null)
            {
                return BadRequest(ApiResponse.Fail("no_employee_link", "目前帳號尚未綁定員工資料"));
            }

            targetEmployeeId = myEmployeeId;
        }

        var query = db.OvertimeRequests
            .AsNoTracking()
            .Include(o => o.Employee)
            .Include(o => o.Approver)
            .AsQueryable();

        if (targetEmployeeId is not null)
        {
            query = query.Where(o => o.EmployeeId == targetEmployeeId);
        }
        else if (scopeEmployeeIds is not null)
        {
            query = query.Where(o => scopeEmployeeIds.Contains(o.EmployeeId));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OvertimeRequestDto(
                o.Id, o.EmployeeId,
                o.Employee != null ? o.Employee.Name : null,
                o.Employee != null ? o.Employee.EmployeeNo : string.Empty,
                o.WorkDate, o.StartAt, o.EndAt, o.Hours, o.Reason, o.Status, o.ApproverId,
                o.Approver != null ? o.Approver.Name : null,
                o.CreatedAt))
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(new PagedResult<OvertimeRequestDto>(items, total, page, pageSize)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOvertimeRequest request, CancellationToken ct)
    {
        var employeeId = await CurrentEmployeeIdAsync(ct);
        if (employeeId is null)
        {
            return BadRequest(ApiResponse.Fail("no_employee_link", "目前帳號尚未綁定員工資料"));
        }

        if (request.EndAt <= request.StartAt)
        {
            return BadRequest(ApiResponse.Fail("invalid_period", "結束時間必須晚於開始時間"));
        }

        var overtime = new OvertimeRequest
        {
            EmployeeId = employeeId.Value,
            WorkDate = request.WorkDate,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Hours = Math.Round((decimal)(request.EndAt - request.StartAt).TotalHours, 2),
            Reason = request.Reason,
            Status = "pending"
        };
        db.OvertimeRequests.Add(overtime);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("create", "overtime", "overtime_request", overtime.Id,
            new { employeeId, overtime.WorkDate, overtime.Hours }, ct);

        var saved = await LoadAsync(overtime.Id, ct);
        return Ok(ApiResponse.Ok(ToDto(saved)));
    }

    [HttpPut("{id:long}/approve")]
    [Authorize(Policy = PermissionCatalog.OvertimeApprove)]
    public async Task<IActionResult> Review(long id, ReviewRequest request, CancellationToken ct)
    {
        var overtime = await db.OvertimeRequests.SingleOrDefaultAsync(o => o.Id == id, ct);
        if (overtime is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "加班單不存在"));
        }

        if (overtime.Status != "pending")
        {
            return BadRequest(ApiResponse.Fail("already_reviewed", "此加班單已審核過"));
        }

        if (request.Action is not ("approve" or "reject"))
        {
            return BadRequest(ApiResponse.Fail("invalid_action", "action 必須是 approve 或 reject"));
        }

        var (error, approverEmployeeId) = await ResolveApproverAsync(overtime.EmployeeId, ct);
        if (error is not null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("forbidden", error));
        }

        overtime.Status = request.Action == "approve" ? "approved" : "rejected";
        overtime.ApproverId = approverEmployeeId;
        overtime.ApprovedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(request.Action == "approve" ? "approve" : "reject", "overtime", "overtime_request", id,
            new { employeeId = overtime.EmployeeId }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    [HttpPut("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var employeeId = await CurrentEmployeeIdAsync(ct);
        var overtime = await db.OvertimeRequests.SingleOrDefaultAsync(o => o.Id == id, ct);
        if (overtime is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "加班單不存在"));
        }

        if (overtime.EmployeeId != employeeId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("forbidden", "只能取消自己的申請"));
        }

        if (overtime.Status != "pending")
        {
            return BadRequest(ApiResponse.Fail("not_cancellable", "只有待審核的申請可以取消"));
        }

        overtime.Status = "cancelled";
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("cancel", "overtime", "overtime_request", id,
            new { employeeId = overtime.EmployeeId }, ct);
        return Ok(ApiResponse.Ok(true));
    }

    private async Task<string?> EnsureCanViewOthersAsync(long targetEmployeeId, CancellationToken ct)
    {
        if (IsHrAdmin)
        {
            return null;
        }

        var myEmployeeId = await CurrentEmployeeIdAsync(ct);
        if (myEmployeeId is null)
        {
            return "目前帳號未綁定員工資料";
        }

        var isManager = await db.Employees.AnyAsync(e => e.Id == targetEmployeeId && e.ManagerId == myEmployeeId, ct);
        return isManager ? null : "只能查看自己或部屬的申請";
    }

    private async Task<OvertimeRequest?> LoadAsync(long id, CancellationToken ct) =>
        await db.OvertimeRequests
            .AsNoTracking()
            .Include(o => o.Employee)
            .Include(o => o.Approver)
            .SingleOrDefaultAsync(o => o.Id == id, ct);

    private static OvertimeRequestDto ToDto(OvertimeRequest o) =>
        new(
            o.Id, o.EmployeeId,
            o.Employee != null ? o.Employee.Name : null,
            o.Employee != null ? o.Employee.EmployeeNo : string.Empty,
            o.WorkDate, o.StartAt, o.EndAt, o.Hours, o.Reason, o.Status, o.ApproverId,
            o.Approver != null ? o.Approver.Name : null,
            o.CreatedAt);
}