using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/leave-types")]
[Authorize]
public class LeaveTypesController(HrDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await db.LeaveTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Id)
            .Select(t => new LeaveTypeDto(t.Id, t.Code, t.Name, t.AnnualQuota, t.IsPaid, t.IsActive))
            .ToListAsync(ct);
        return Ok(ApiResponse.Ok(items));
    }
}