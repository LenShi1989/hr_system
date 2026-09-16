using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/payrolls")]
[Authorize]
public class PayrollsController(HrDbContext db, PayrollCalculator calculator) : ApiControllerBase(db)
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? period = null,
        [FromQuery] long? employeeId = null,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (employeeId is not null)
        {
            var scopeError = await EnsureCanViewEmployeeAsync(employeeId.Value, ct);
            if (scopeError is not null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("forbidden", scopeError));
            }
        }

        var query = db.Payrolls.AsNoTracking().Include(p => p.Employee).AsQueryable();

        if (period is not null)
        {
            query = query.Where(p => p.Period == period);
        }

        if (employeeId is not null)
        {
            query = query.Where(p => p.EmployeeId == employeeId);
        }
        else if (!IsHrAdmin)
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

            var scope = managed.Append(myEmployeeId.Value).ToList();
            query = query.Where(p => scope.Contains(p.EmployeeId));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.Period)
            .ThenBy(p => p.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(new PagedResult<PayrollDto>(items, total, page, pageSize)));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken ct)
    {
        var payroll = await db.Payrolls
            .AsNoTracking()
            .Include(p => p.Employee)
            .Include(p => p.GeneratedByUser)
            .SingleOrDefaultAsync(p => p.Id == id, ct);

        if (payroll is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "薪資單不存在"));
        }

        var scopeError = await EnsureCanViewEmployeeAsync(payroll.EmployeeId, ct);
        if (scopeError is not null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("forbidden", scopeError));
        }

        return Ok(ApiResponse.Ok(ToDto(payroll)));
    }

    [HttpPost("generate")]
    [Authorize(Policy = PermissionCatalog.PayrollManage)]
    public async Task<IActionResult> Generate(GeneratePayrollRequest request, CancellationToken ct)
    {
        if (!IsValidPeriod(request.Period))
        {
            return BadRequest(ApiResponse.Fail("invalid_period", "period 格式必須為 YYYYMM"));
        }

        var result = await calculator.GenerateAsync(request.Period, CurrentUserId, ct);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("{id:long}/confirm")]
    [Authorize(Policy = PermissionCatalog.PayrollManage)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        var payroll = await db.Payrolls.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (payroll is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "薪資單不存在"));
        }

        if (payroll.Status != "draft")
        {
            return BadRequest(ApiResponse.Fail("not_draft", "只有草稿狀態的薪資單可以確認"));
        }

        payroll.Status = "confirmed";
        payroll.ConfirmedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok(true));
    }

    [HttpPut("{id:long}/pay")]
    [Authorize(Policy = PermissionCatalog.PayrollManage)]
    public async Task<IActionResult> Pay(long id, CancellationToken ct)
    {
        var payroll = await db.Payrolls.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (payroll is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "薪資單不存在"));
        }

        if (payroll.Status != "confirmed")
        {
            return BadRequest(ApiResponse.Fail("not_confirmed", "只有已確認的薪資單可以發放"));
        }

        payroll.Status = "paid";
        payroll.PaidAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok(true));
    }

    [HttpPut("{id:long}/bonus")]
    [Authorize(Policy = PermissionCatalog.PayrollManage)]
    public async Task<IActionResult> Bonus(long id, SetBonusRequest request, CancellationToken ct)
    {
        var payroll = await db.Payrolls.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (payroll is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "薪資單不存在"));
        }

        if (payroll.Status != "draft")
        {
            return BadRequest(ApiResponse.Fail("locked", "已確認或已發放的薪資單不可調整獎金"));
        }

        var oldBonus = payroll.Bonus;
        payroll.Bonus = request.Amount;
        payroll.GrossPay = payroll.GrossPay - oldBonus + request.Amount;
        payroll.NetPay = payroll.GrossPay - payroll.InsuranceDeduction - payroll.TaxWithheld - payroll.LeaveDeduction;
        await db.SaveChangesAsync(ct);
        return Ok(ApiResponse.Ok(true));
    }

    private async Task<string?> EnsureCanViewEmployeeAsync(long targetEmployeeId, CancellationToken ct)
    {
        if (IsHrAdmin)
        {
            return null;
        }

        var myEmployeeId = await CurrentEmployeeIdAsync(ct);
        if (myEmployeeId == targetEmployeeId)
        {
            return null;
        }

        if (myEmployeeId is null)
        {
            return "目前帳號未綁定員工資料";
        }

        var isManager = await db.Employees.AnyAsync(e => e.Id == targetEmployeeId && e.ManagerId == myEmployeeId, ct);
        return isManager ? null : "只能查看本人或部屬的薪資單";
    }

    private static bool IsValidPeriod(int period)
    {
        var month = period % 100;
        var year = period / 100;
        return month is >= 1 and <= 12 && year is >= 2000 and <= 2100;
    }

    private static PayrollDto ToDto(Payroll p) =>
        new(
            p.Id, p.Period, p.EmployeeId,
            p.Employee != null ? p.Employee.Name : null,
            p.Employee != null ? p.Employee.EmployeeNo : string.Empty,
            p.BaseAmount, p.OvertimePay, p.Bonus, p.LeaveDeduction,
            p.InsuranceDeduction, p.TaxWithheld, p.GrossPay, p.NetPay,
            p.Status, p.GeneratedBy,
            p.GeneratedByUser != null ? p.GeneratedByUser.Email : null,
            p.GeneratedAt, p.ConfirmedAt, p.PaidAt);
}