using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/departments")]
[Authorize]
public class DepartmentsController(HrDbContext db, AuditLogService audit) : ControllerBase
{
    [HttpGet("tree")]
    [Authorize(Policy = PermissionCatalog.EmployeeRead)]
    public async Task<IActionResult> Tree(CancellationToken ct)
    {
        var all = await db.Departments.AsNoTracking().OrderBy(d => d.Code).ToListAsync(ct);
        var roots = all.Where(d => d.ParentId is null).Select(d => BuildNode(d, all)).ToList();
        return Ok(ApiResponse.Ok(roots));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = PermissionCatalog.EmployeeRead)]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var d = await db.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (d is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "部門不存在"));
        }

        return Ok(ApiResponse.Ok(ToDto(d)));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Create(DepartmentUpsertRequest request, CancellationToken ct)
    {
        var error = await ValidateAsync(request, ct);
        if (error is not null)
        {
            return BadRequest(ApiResponse.Fail("validation_error", error));
        }

        var department = new Department
        {
            Code = request.Code,
            Name = request.Name,
            ParentId = request.ParentId,
            ManagerId = request.ManagerId
        };
        db.Departments.Add(department);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("create", "organization", "department", department.Id,
            new { department.Code, department.Name }, ct);

        return Ok(ApiResponse.Ok(ToDto(department)));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Update(long id, DepartmentUpsertRequest request, CancellationToken ct)
    {
        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == id, ct);
        if (department is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "部門不存在"));
        }

        if (request.ParentId == id)
        {
            return BadRequest(ApiResponse.Fail("validation_error", "上層部門不能是自己"));
        }

        var error = await ValidateAsync(request, ct, id);
        if (error is not null)
        {
            return BadRequest(ApiResponse.Fail("validation_error", error));
        }

        department.Code = request.Code;
        department.Name = request.Name;
        department.ParentId = request.ParentId;
        department.ManagerId = request.ManagerId;
        department.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("update", "organization", "department", id,
            new { department.Code, department.Name }, ct);

        return Ok(ApiResponse.Ok(ToDto(department)));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == id, ct);
        if (department is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "部門不存在"));
        }

        var hasChildren = await db.Departments.AnyAsync(d => d.ParentId == id, ct);
        if (hasChildren)
        {
            return BadRequest(ApiResponse.Fail("has_children", "部門下仍有子部門，無法刪除"));
        }

        var hasEmployees = await db.Employees.AnyAsync(e => e.DepartmentId == id && e.IsActive, ct);
        if (hasEmployees)
        {
            return BadRequest(ApiResponse.Fail("has_employees", "部門下仍有在職員工，無法刪除"));
        }

        department.IsActive = false;
        department.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("delete", "organization", "department", id,
            new { department.Code, department.Name }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    private async Task<string?> ValidateAsync(DepartmentUpsertRequest request, CancellationToken ct, long? excludeId = null)
    {
        var codeTaken = await db.Departments.AnyAsync(d => d.Code == request.Code && d.Id != excludeId, ct);
        if (codeTaken)
        {
            return "部門代碼已存在";
        }

        if (request.ParentId is long parentId)
        {
            var parent = await db.Departments.AnyAsync(d => d.Id == parentId, ct);
            if (!parent)
            {
                return "上層部門不存在";
            }
        }

        return null;
    }

    private static DepartmentDto ToDto(Department d) =>
        new(d.Id, d.ParentId, d.Code, d.Name, d.ManagerId, d.IsActive, []);

    private static DepartmentDto BuildNode(Department node, IReadOnlyList<Department> all) =>
        new(
            node.Id,
            node.ParentId,
            node.Code,
            node.Name,
            node.ManagerId,
            node.IsActive,
            all.Where(c => c.ParentId == node.Id)
               .OrderBy(c => c.Code)
               .Select(c => BuildNode(c, all))
               .ToList());
}