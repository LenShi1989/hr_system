using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public class EmployeesController(HrDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.EmployeeRead)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] long? departmentId = null,
        [FromQuery] string? employmentStatus = null,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(e =>
                e.EmployeeNo.Contains(keyword) || e.Name.Contains(keyword) || (e.Email != null && e.Email.Contains(keyword)));
        }

        if (departmentId is not null)
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        if (!string.IsNullOrWhiteSpace(employmentStatus))
        {
            query = query.Where(e => e.EmploymentStatus == employmentStatus);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.EmployeeNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => ToDto(e))
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(new PagedResult<EmployeeDto>(items, total, page, pageSize)));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = PermissionCatalog.EmployeeRead)]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.Manager)
            .SingleOrDefaultAsync(e => e.Id == id, ct);

        if (employee is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "員工不存在"));
        }

        return Ok(ApiResponse.Ok(ToDto(employee)));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Create(EmployeeUpsertRequest request, CancellationToken ct)
    {
        var error = await ValidateAsync(request, ct);
        if (error is not null)
        {
            return BadRequest(ApiResponse.Fail("validation_error", error));
        }

        var employee = new Employee
        {
            EmployeeNo = request.EmployeeNo,
            Name = request.Name,
            Gender = request.Gender,
            BirthDate = request.BirthDate,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            HireDate = request.HireDate,
            LeaveDate = request.LeaveDate,
            EmploymentStatus = request.EmploymentStatus,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            ManagerId = request.ManagerId
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(ToDto(employee)));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Update(long id, EmployeeUpsertRequest request, CancellationToken ct)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "員工不存在"));
        }

        var error = await ValidateAsync(request, ct, id);
        if (error is not null)
        {
            return BadRequest(ApiResponse.Fail("validation_error", error));
        }

        employee.EmployeeNo = request.EmployeeNo;
        employee.Name = request.Name;
        employee.Gender = request.Gender;
        employee.BirthDate = request.BirthDate;
        employee.Phone = request.Phone;
        employee.Email = request.Email;
        employee.Address = request.Address;
        employee.HireDate = request.HireDate;
        employee.LeaveDate = request.LeaveDate;
        employee.EmploymentStatus = request.EmploymentStatus;
        employee.DepartmentId = request.DepartmentId;
        employee.PositionId = request.PositionId;
        employee.ManagerId = request.ManagerId;
        employee.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(ToDto(employee)));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "員工不存在"));
        }

        var hasSubordinates = await db.Employees.AnyAsync(e => e.ManagerId == id && e.IsActive, ct);
        if (hasSubordinates)
        {
            return BadRequest(ApiResponse.Fail("has_subordinates", "仍有員工以此人為主管，無法刪除"));
        }

        employee.IsActive = false;
        employee.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(true));
    }

    private async Task<string?> ValidateAsync(EmployeeUpsertRequest request, CancellationToken ct, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(request.EmployeeNo) || string.IsNullOrWhiteSpace(request.Name))
        {
            return "工號與姓名為必填";
        }

        var noTaken = await db.Employees.AnyAsync(e => e.EmployeeNo == request.EmployeeNo && e.Id != excludeId, ct);
        if (noTaken)
        {
            return "工號已存在";
        }

        var deptOk = await db.Departments.AnyAsync(d => d.Id == request.DepartmentId && d.IsActive, ct);
        if (!deptOk)
        {
            return "部門不存在或已停用";
        }

        var posOk = await db.Positions.AnyAsync(p => p.Id == request.PositionId && p.IsActive, ct);
        if (!posOk)
        {
            return "職位不存在或已停用";
        }

        if (request.ManagerId is long managerId && managerId != excludeId &&
            !await db.Employees.AnyAsync(e => e.Id == managerId && e.IsActive, ct))
        {
            return "主管不存在或已停用";
        }

        return null;
    }

    private static EmployeeDto ToDto(Employee e) =>
        new(
            e.Id, e.EmployeeNo, e.Name, e.Gender, e.BirthDate, e.Phone, e.Email, e.Address,
            e.HireDate, e.LeaveDate, e.EmploymentStatus,
            e.DepartmentId, e.Department is null ? string.Empty : e.Department.Name,
            e.PositionId, e.Position is null ? string.Empty : e.Position.Name,
            e.ManagerId, e.Manager is null ? null : e.Manager.Name,
            e.IsActive, e.CreatedAt);
}