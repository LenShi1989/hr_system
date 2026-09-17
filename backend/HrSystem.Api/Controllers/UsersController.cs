using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = PermissionCatalog.UserManage)]
public class UsersController(HrDbContext db, PasswordHasher hasher, AuditLogService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] long? roleId = null,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u => u.Email.Contains(keyword) ||
                                     (u.Employee != null && (u.Employee.Name.Contains(keyword) || u.Employee.EmployeeNo.Contains(keyword))));
        }

        if (roleId is not null)
        {
            query = query.Where(u => u.RoleId == roleId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto(
                u.Id, u.Email, u.EmployeeId,
                u.Employee != null ? u.Employee.Name : null,
                u.Employee != null ? u.Employee.EmployeeNo : null,
                u.RoleId, u.Role != null ? u.Role.Code : string.Empty,
                u.Role != null ? u.Role.Name : string.Empty,
                u.IsActive, u.CreatedAt))
            .ToListAsync(ct);

        return Ok(ApiResponse.Ok(new PagedResult<UserDto>(items, total, page, pageSize)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var error = await ValidateAsync(request.Email, request.RoleId, request.EmployeeId, null, ct);
        if (error is not null)
        {
            return BadRequest(ApiResponse.Fail("validation_error", error));
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(ApiResponse.Fail("weak_password", "密碼至少 8 個字元"));
        }

        var email = request.Email.Trim();
        var user = new User
        {
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            RoleId = request.RoleId,
            EmployeeId = request.EmployeeId,
            IsActive = request.IsActive
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync("create", "user", "user", user.Id, new { email }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return NotFound(ApiResponse.Fail("not_found", "使用者不存在"));
        }

        var error = await ValidateAsync(request.Email, request.RoleId, request.EmployeeId, id, ct);
        if (error is not null)
        {
            return BadRequest(ApiResponse.Fail("validation_error", error));
        }

        var email = request.Email.Trim();
        var oldIsActive = user.IsActive;
        var oldRoleId = user.RoleId;

        user.Email = email;
        user.RoleId = request.RoleId;
        user.EmployeeId = request.EmployeeId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 8)
            {
                return BadRequest(ApiResponse.Fail("weak_password", "密碼至少 8 個字元"));
            }

            user.PasswordHash = hasher.Hash(request.Password);
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("update", "user", "user", id,
            new { email, roleId = user.RoleId, employeeId = user.EmployeeId, isActive = user.IsActive, passwordReset = !string.IsNullOrWhiteSpace(request.Password) }, ct);

        return Ok(ApiResponse.Ok(true));
    }

    private async Task<string?> ValidateAsync(string email, long roleId, long? employeeId, long? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "請填寫有效的信箱";
        }

        var emailTaken = await db.Users.AnyAsync(u => u.Email == email.Trim() && u.Id != excludeId, ct);
        if (emailTaken)
        {
            return "信箱已被使用";
        }

        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct))
        {
            return "角色不存在";
        }

        if (employeeId is long eid && !await db.Employees.AnyAsync(e => e.Id == eid && e.IsActive, ct))
        {
            return "員工不存在或已停用";
        }

        return null;
    }
}