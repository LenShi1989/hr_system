using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/employee-salaries")]
[Authorize(Policy = PermissionCatalog.PayrollManage)]
public class EmployeeSalariesController(HrDbContext db, AuditLogService audit) : ApiControllerBase(db)
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.EmployeeSalaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .AsQueryable();

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => ToDto(s))
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(new PagedResult<EmployeeSalaryDto>(items, total, page, pageSize)));
    }

    [HttpGet("{employeeId:long}")]
    public async Task<IActionResult> Get(long employeeId, CancellationToken ct)
    {
        var salary = await db.EmployeeSalaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .SingleOrDefaultAsync(s => s.EmployeeId == employeeId, ct);

        if (salary is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "該員工尚未設定薪資結構"));
        }

        return Ok(ApiResponse.Ok(ToDto(salary)));
    }

    [HttpPut("{employeeId:long}")]
    public async Task<IActionResult> Upsert(long employeeId, UpsertEmployeeSalary request, CancellationToken ct)
    {
        if (request.BaseSalary < 0 || request.PositionAllowance < 0 || request.MealAllowance < 0)
        {
            return BadRequest(ApiResponse.Fail("invalid_amount", "薪資與津貼不可為負值"));
        }

        if (request.EffectiveDate == default)
        {
            return BadRequest(ApiResponse.Fail("invalid_effective_date", "請填寫生效日"));
        }

        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "員工不存在"));
        }

        var salary = await db.EmployeeSalaries
            .SingleOrDefaultAsync(s => s.EmployeeId == employeeId, ct);

        var isNew = salary is null;

        if (salary is null)
        {
            salary = new EmployeeSalary { EmployeeId = employeeId };
            db.EmployeeSalaries.Add(salary);
        }

        salary.BaseSalary = request.BaseSalary;
        salary.PositionAllowance = request.PositionAllowance;
        salary.MealAllowance = request.MealAllowance;
        salary.EffectiveDate = request.EffectiveDate;
        salary.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(isNew ? "create" : "update", "salary", "employee_salary", employeeId,
            new { salary.BaseSalary, salary.PositionAllowance, salary.MealAllowance, salary.EffectiveDate }, ct);

        var saved = await db.EmployeeSalaries
            .AsNoTracking()
            .Include(s => s.Employee)
            .SingleAsync(s => s.EmployeeId == employeeId, ct);
        return Ok(ApiResponse.Ok(ToDto(saved)));
    }

    private static EmployeeSalaryDto ToDto(EmployeeSalary s) =>
        new(
            s.EmployeeId,
            s.Employee != null ? s.Employee.Name : null,
            s.Employee != null ? s.Employee.EmployeeNo : string.Empty,
            s.BaseSalary, s.PositionAllowance, s.MealAllowance, s.EffectiveDate, s.UpdatedAt);
}