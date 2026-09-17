namespace HrSystem.Api.Dtos;

public record ApiError(string Code, string Message);

public class ApiResponse
{
    public object? Data { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResponse Ok(object? data) => new() { Data = data };
    public static ApiResponse Fail(string code, string message) => new() { Error = new ApiError(code, message) };
}

public record LoginRequest(string Email, string Password);

public record CurrentUserDto(long Id, string Email, string RoleCode, string[] Permissions);

public class LoginResponse
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required int ExpiresIn { get; init; }
    public required CurrentUserDto User { get; init; }
}