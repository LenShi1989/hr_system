using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Policy = PermissionCatalog.UserManage)]
public class RolesController(HrDbContext db) : ControllerBase
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
}