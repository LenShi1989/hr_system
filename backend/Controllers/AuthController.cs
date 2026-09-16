using System.Security.Claims;
using HrSystem.Api.Data;
using HrSystem.Api.Dtos;
using HrSystem.Api.Models;
using HrSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrSystem.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly HrDbContext _db;
    private readonly PasswordHasher _hasher;
    private readonly TokenService _tokenService;

    public AuthController(HrDbContext db, PasswordHasher hasher, TokenService tokenService)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role!)
            .ThenInclude(r => r.Permissions)
            .SingleOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || !user.IsActive || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(ApiResponse.Fail("invalid_credentials", "信箱或密碼錯誤"));
        }

        var token = _tokenService.CreateToken(user);

        return Ok(ApiResponse.Ok(new LoginResponse
        {
            AccessToken = token.AccessToken,
            TokenType = token.TokenType,
            ExpiresIn = token.ExpiresIn,
            User = ToCurrentUser(user)
        }));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role!)
            .ThenInclude(r => r.Permissions)
            .SingleOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(ApiResponse.Fail("invalid_token", "使用者不存在或已停用"));
        }

        return Ok(ApiResponse.Ok(ToCurrentUser(user)));
    }

    private static CurrentUserDto ToCurrentUser(User user) =>
        new(
            user.Id,
            user.Email,
            user.Role?.Code ?? string.Empty,
            user.Role?.Permissions.Select(p => p.PermissionCode).ToArray() ?? []);
}