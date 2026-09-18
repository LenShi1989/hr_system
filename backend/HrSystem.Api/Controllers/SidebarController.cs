using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/sidebar")]
[Authorize]
public class SidebarController(HrDbContext db) : ControllerBase
{
    [HttpGet("menus")]
    public async Task<IActionResult> Menus(CancellationToken ct)
    {
        var permissions = User.FindAll(TokenService.PermissionClaimType)
            .Select(c => c.Value)
            .ToHashSet();

        var menus = await db.SidebarMenus
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.GroupOrder)
            .ThenBy(m => m.SortOrder)
            .ToListAsync(ct);

        var items = menus
            .Where(m => permissions.Contains(m.PermissionCode))
            .Select(m => new SidebarMenuItemDto(
                m.GroupTitle,
                m.GroupOrder,
                m.Label,
                m.Route,
                m.Icon,
                m.PermissionCode,
                m.SortOrder))
            .ToList();

        return Ok(ApiResponse.Ok(items));
    }
}