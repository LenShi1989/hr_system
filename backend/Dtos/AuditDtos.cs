using HrSystem.Api.Models;

namespace HrSystem.Api.Dtos;

public record AuditLogDto(
    long Id,
    long UserId,
    string UserEmail,
    string Role,
    string Action,
    string Category,
    string Entity,
    long? EntityId,
    string? Detail,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public record AuditQuery(int Page = 1, int PageSize = 20, string? Category = null, string? Action = null, long? UserId = null, DateOnly? FromDate = null, DateOnly? ToDate = null);

public static class AuditLogMapper
{
    public static AuditLogDto ToDto(AuditLog a) =>
        new(
            a.Id, a.UserId, a.UserEmail, a.Role, a.Action, a.Category, a.Entity,
            a.EntityId, a.Detail, a.IpAddress, a.CreatedAt);
}