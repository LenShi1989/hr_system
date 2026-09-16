using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/positions")]
[Authorize]
public class PositionsController(HrDbContext db, AuditLogService audit) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.EmployeeRead)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await db.Positions
            .AsNoTracking()
            .Include(p => p.Department)
            .OrderBy(p => p.Code)
            .Select(p => new PositionDto(
                p.Id, p.Code, p.Name, p.DepartmentId,
                p.Department != null ? p.Department.Name : null,
                p.Level, p.IsActive))
            .ToListAsync(ct);
        return Ok(ApiResponse.Ok(items));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Create(PositionUpsertRequest request, CancellationToken ct)
    {
        var error = await ValidateAsync(request, ct);
        if (error is not null)
        {
            return BadRequest(ApiResponse.Fail("validation_error", error));
        }

        var position = new Position
        {
            Code = request.Code,
            Name = request.Name,
            DepartmentId = request.DepartmentId,
            Level = request.Level
        };
        db.Positions.Add(position);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("create", "organization", "position", position.Id,
            new { position.Code, position.Name }, ct);

        return Ok(ApiResponse.Ok(ToDto(position)));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Update(long id, PositionUpsertRequest request, CancellationToken ct)
    {
        var position = await db.Positions.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (position is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "職位不存在"));
        }

        var error = await ValidateAsync(request, ct, id);
        if (error is not null)
        {
            return BadRequest(ApiResponse.Fail("validation_error", error));
        }

        position.Code = request.Code;
        position.Name = request.Name;
        position.DepartmentId = request.DepartmentId;
        position.Level = request.Level;
        position.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("update", "organization", "position", id,
            new { position.Code, position.Name }, ct);

        return Ok(ApiResponse.Ok(ToDto(position)));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = PermissionCatalog.EmployeeManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var position = await db.Positions.SingleOrDefaultAsync(p => p.Id == id, ct);
        if (position is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "職位不存在"));
        }

        var inUse = await db.Employees.AnyAsync(e => e.PositionId == id && e.IsActive, ct);
        if (inUse)
        {
            return BadRequest(ApiResponse.Fail("in_use", "仍有員工使用此職位，無法刪除"));
        }

        position.IsActive = false;
        position.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("delete", "organization", "position", id,
            new { position.Code, position.Name }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    private async Task<string?> ValidateAsync(PositionUpsertRequest request, CancellationToken ct, long? excludeId = null)
    {
        var codeTaken = await db.Positions.AnyAsync(p => p.Code == request.Code && p.Id != excludeId, ct);
        if (codeTaken)
        {
            return "職位代碼已存在";
        }

        if (request.DepartmentId is long deptId &&
            !await db.Departments.AnyAsync(d => d.Id == deptId, ct))
        {
            return "所屬部門不存在";
        }

        return null;
    }

    private static PositionDto ToDto(Position p) =>
        new(p.Id, p.Code, p.Name, p.DepartmentId, p.Department?.Name, p.Level, p.IsActive);
}