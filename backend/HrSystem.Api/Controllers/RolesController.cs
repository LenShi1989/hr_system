using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Policy = PermissionCatalog.UserManage)]
public class RolesController(HrDbContext db, AuditLogService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

        var userCounts = await db.Users
            .AsNoTracking()
            .GroupBy(u => u.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countMap = userCounts.ToDictionary(c => c.RoleId, c => c.Count);

        var items = roles
            .Select(r => new RoleDto(
                r.Id,
                r.Code,
                r.Name,
                r.IsActive,
                countMap.TryGetValue(r.Id, out var count) ? count : 0,
                r.Permissions.OrderBy(p => p.PermissionCode).Select(p => p.PermissionCode).ToArray()))
            .ToList();

        return Ok(ApiResponse.Ok(items));
    }

    [HttpGet("permissions")]
    public IActionResult Permissions()
    {
        var items = PermissionCatalog.Definitions
            .Select(d => new PermissionDto(d.Code, d.Label, d.Group))
            .ToList();

        return Ok(ApiResponse.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var code = request.Code?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse.Fail("validation_error", "角色代碼與名稱為必填"));
        }

        if (await db.Roles.AnyAsync(r => r.Code == code, ct))
        {
            return BadRequest(ApiResponse.Fail("code_taken", "角色代碼已存在"));
        }

        var permissions = NormalizePermissions(request.PermissionCodes);
        if (permissions is null)
        {
            return BadRequest(ApiResponse.Fail("invalid_permission", "包含無效的權限代碼"));
        }

        var role = new Role { Code = code, Name = request.Name.Trim() };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        foreach (var pc in permissions)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = pc });
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("create", "role", "role", role.Id,
            new { role.Code, role.Name, permissions }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

        if (role is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "角色不存在"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse.Fail("validation_error", "名稱為必填"));
        }

        var permissions = NormalizePermissions(request.PermissionCodes);
        if (permissions is null)
        {
            return BadRequest(ApiResponse.Fail("invalid_permission", "包含無效的權限代碼"));
        }

        role.Name = request.Name.Trim();

        var current = role.Permissions.Select(p => p.PermissionCode).ToHashSet();
        foreach (var existing in role.Permissions.Where(p => !permissions.Contains(p.PermissionCode)).ToList())
        {
            db.RolePermissions.Remove(existing);
        }

        foreach (var pc in permissions.Except(current))
        {
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = pc });
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("update", "role", "role", role.Id,
            new { role.Code, role.Name, permissions }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .SingleOrDefaultAsync(r => r.Id == id, ct);

        if (role is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "角色不存在"));
        }

        var userCount = await db.Users.CountAsync(u => u.RoleId == id, ct);
        if (userCount > 0)
        {
            return BadRequest(ApiResponse.Fail("role_in_use", $"仍有 {userCount} 位使用者綁定此角色，請先調整後再刪除"));
        }

        var code = role.Code;
        var name = role.Name;
        db.RolePermissions.RemoveRange(role.Permissions);
        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("delete", "role", "role", id, new { code, name }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    private static string[]? NormalizePermissions(string[]? codes)
    {
        var known = PermissionCatalog.AllCodes.ToHashSet();
        var result = (codes ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct()
            .ToArray();

        return result.All(known.Contains) ? result : null;
    }
}