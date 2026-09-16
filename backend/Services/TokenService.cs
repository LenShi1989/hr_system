using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HrSystem.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace HrSystem.Api.Services;

public class TokenService(IConfiguration config)
{
    public const string PermissionClaimType = "permission";

    public TokenResponse CreateToken(User user)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");
        var expiresHours = config.GetValue("Jwt:ExpiresHours", 8);
        var now = DateTimeOffset.UtcNow;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.EmployeeId is long employeeId)
        {
            claims.Add(new Claim("employee_id", employeeId.ToString()));
        }

        if (user.Role is not null && user.Role.Permissions is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.Code));
            foreach (var code in user.Role.Permissions.Select(p => p.PermissionCode))
            {
                claims.Add(new Claim(PermissionClaimType, code));
            }
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddHours(expiresHours).UtcDateTime,
            signingCredentials: credentials);

        return new TokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresIn = (int)TimeSpan.FromHours(expiresHours).TotalSeconds
        };
    }
}

public class TokenResponse
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
}