using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/leave-requests")]
[Authorize]
public class LeaveRequestsController(HrDbContext db, AuditLogService audit) : ApiControllerBase(db)
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

        var query = db.LeaveRequests
            .AsNoTracking()
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .Include(l => l.Approver)
            .AsQueryable();

        if (targetEmployeeId is not null)
        {
            query = query.Where(l => l.EmployeeId == targetEmployeeId);
        }
        else if (scopeEmployeeIds is not null)
        {
            query = query.Where(l => scopeEmployeeIds.Contains(l.EmployeeId));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeaveRequestDto(
                l.Id, l.EmployeeId,
                l.Employee != null ? l.Employee.Name : null,
                l.Employee != null ? l.Employee.EmployeeNo : string.Empty,
                l.LeaveTypeId,
                l.LeaveType != null ? l.LeaveType.Name : null,
                l.StartAt, l.EndAt, l.Days, l.Reason, l.Status, l.ApproverId,
                l.Approver != null ? l.Approver.Name : null,
                l.CreatedAt))
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(new PagedResult<LeaveRequestDto>(items, total, page, pageSize)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLeaveRequest request, CancellationToken ct)
    {
        var employeeId = await CurrentEmployeeIdAsync(ct);
        if (employeeId is null)
        {
            return BadRequest(ApiResponse.Fail("no_employee_link", "目前帳號尚未綁定員工資料"));
        }

        var leaveType = await db.LeaveTypes.SingleOrDefaultAsync(t => t.Id == request.LeaveTypeId && t.IsActive, ct);
        if (leaveType is null)
        {
            return BadRequest(ApiResponse.Fail("invalid_leave_type", "假別不存在或已停用"));
        }

        if (request.EndAt <= request.StartAt)
        {
            return BadRequest(ApiResponse.Fail("invalid_period", "結束時間必須晚於開始時間"));
        }

        var days = (decimal)(request.EndAt - request.StartAt).TotalDays;
        var leave = new LeaveRequest
        {
            EmployeeId = employeeId.Value,
            LeaveTypeId = request.LeaveTypeId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Days = Math.Round(days, 2),
            Reason = request.Reason,
            Status = "pending"
        };
        db.LeaveRequests.Add(leave);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("create", "leave", "leave_request", leave.Id,
            new { employeeId, leave.LeaveTypeId, leave.Days }, ct);

        var saved = await LoadAsync(leave.Id, ct);
        return Ok(ApiResponse.Ok(ToDto(saved)));
    }

    [HttpPut("{id:long}/approve")]
    [Authorize(Policy = PermissionCatalog.LeaveApprove)]
    public async Task<IActionResult> Review(long id, ReviewRequest request, CancellationToken ct)
    {
        var leave = await db.LeaveRequests.SingleOrDefaultAsync(l => l.Id == id, ct);
        if (leave is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "請假單不存在"));
        }

        if (leave.Status != "pending")
        {
            return BadRequest(ApiResponse.Fail("already_reviewed", "此請假單已審核過"));
        }

        if (request.Action is not ("approve" or "reject"))
        {
            return BadRequest(ApiResponse.Fail("invalid_action", "action 必須是 approve 或 reject"));
        }

        var (error, approverEmployeeId) = await ResolveApproverAsync(leave.EmployeeId, ct);
        if (error is not null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("forbidden", error));
        }

        leave.Status = request.Action == "approve" ? "approved" : "rejected";
        leave.ApproverId = approverEmployeeId;
        leave.ApprovedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(request.Action == "approve" ? "approve" : "reject", "leave", "leave_request", id,
            new { employeeId = leave.EmployeeId }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    [HttpPut("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var employeeId = await CurrentEmployeeIdAsync(ct);
        var leave = await db.LeaveRequests.SingleOrDefaultAsync(l => l.Id == id, ct);
        if (leave is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "請假單不存在"));
        }

        if (leave.EmployeeId != employeeId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("forbidden", "只能取消自己的申請"));
        }

        if (leave.Status != "pending")
        {
            return BadRequest(ApiResponse.Fail("not_cancellable", "只有待審核的申請可以取消"));
        }

        leave.Status = "cancelled";
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("cancel", "leave", "leave_request", id,
            new { employeeId = leave.EmployeeId }, ct);
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

    private async Task<LeaveRequest?> LoadAsync(long id, CancellationToken ct) =>
        await db.LeaveRequests
            .AsNoTracking()
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .Include(l => l.Approver)
            .SingleOrDefaultAsync(l => l.Id == id, ct);

    private static LeaveRequestDto ToDto(LeaveRequest l) =>
        new(
            l.Id, l.EmployeeId,
            l.Employee != null ? l.Employee.Name : null,
            l.Employee != null ? l.Employee.EmployeeNo : string.Empty,
            l.LeaveTypeId,
            l.LeaveType != null ? l.LeaveType.Name : null,
            l.StartAt, l.EndAt, l.Days, l.Reason, l.Status, l.ApproverId,
            l.Approver != null ? l.Approver.Name : null,
            l.CreatedAt);
}