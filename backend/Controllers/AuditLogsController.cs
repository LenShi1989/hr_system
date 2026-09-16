using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Policy = PermissionCatalog.AuditRead)]
public class AuditLogsController(HrDbContext db) : ApiControllerBase(db)
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? action = null,
        [FromQuery] long? userId = null,
        [FromQuery] string? keyword = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (userId is not null)
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(a => a.UserEmail.Contains(keyword) || a.Entity.Contains(keyword) || a.Role.Contains(keyword));
        }

        if (fromDate is not null)
        {
            query = query.Where(a => a.CreatedAt >= fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified));
        }

        if (toDate is not null)
        {
            query = query.Where(a => a.CreatedAt <= toDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Unspecified));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => AuditLogMapper.ToDto(a))
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(new PagedResult<AuditLogDto>(items, total, page, pageSize)));
    }
}