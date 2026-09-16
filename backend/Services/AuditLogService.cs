using System.Security.Claims;
using System.Text.Json;
using HrSystem.Api.Data;
using HrSystem.Api.Models;

namespace HrSystem.Api.Services;

public class AuditLogService(HrDbContext db, IHttpContextAccessor httpContextAccessor)
{
    public async Task LogAsync(
        string action,
        string category,
        string entity,
        long? entityId = null,
        object? detail = null,
        CancellationToken ct = default)
    {
        var user = httpContextAccessor.HttpContext?.User;

        long userId = 0;
        if (user?.FindFirstValue(ClaimTypes.NameIdentifier) is string id && long.TryParse(id, out var parsed))
        {
            userId = parsed;
        }

        await AddAsync(
            userId,
            user?.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            user?.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            action, category, entity, entityId, detail,
            httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            ct);
    }

    public async Task LogLoginAsync(
        long userId, string email, string role, bool success, string? reason = null, CancellationToken ct = default)
    {
        await AddAsync(
            userId, email, role,
            success ? "login" : "login_failed", "auth", "user", userId,
            reason is null ? null : new { reason }, null, ct);
    }

    private async Task AddAsync(
        long userId,
        string userEmail,
        string role,
        string action,
        string category,
        string entity,
        long? entityId,
        object? detail,
        string? ip,
        CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserEmail = userEmail,
            Role = role,
            Action = action,
            Category = category,
            Entity = entity,
            EntityId = entityId,
            Detail = detail is null ? null : JsonSerializer.Serialize(detail),
            IpAddress = ip,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}