using System.Security.Claims;
using HrSystem.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

public class ApiControllerBase(HrDbContext db) : ControllerBase
{
    protected long CurrentUserId =>
        long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected bool IsHrAdmin =>
        User.IsInRole("hr") || User.IsInRole("admin");

    protected async Task<long?> CurrentEmployeeIdAsync(CancellationToken ct) =>
        (await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == CurrentUserId, ct))?.EmployeeId;

    protected async Task<(string? Error, long? ApproverEmployeeId)> ResolveApproverAsync(
        long requestEmployeeId, CancellationToken ct)
    {
        var myEmployeeId = await CurrentEmployeeIdAsync(ct);

        if (myEmployeeId == requestEmployeeId)
        {
            return ("不能審核自己的申請", myEmployeeId);
        }

        if (IsHrAdmin)
        {
            return (null, myEmployeeId);
        }

        if (myEmployeeId is null)
        {
            return ("目前帳號未綁定員工，無法審核", null);
        }

        var isDirectManager = await db.Employees
            .AnyAsync(e => e.Id == requestEmployeeId && e.ManagerId == myEmployeeId, ct);

        return isDirectManager
            ? (null, myEmployeeId)
            : ("僅直屬主管或 HR 可審核此申請", null);
    }
}