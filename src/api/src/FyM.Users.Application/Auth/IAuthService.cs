namespace FyM.Users.Application.Auth;

public interface IAuthService
{
    Task<LoginResult> RegisterAsync(RegisterRequest request, string? ipAddress, string? userAgent, CancellationToken ct);
    Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken ct);
    Task<LoginResult> RefreshAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken ct);
    Task LogoutAsync(string? refreshToken, string? ipAddress, CancellationToken ct);
    Task<AuthenticatedUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, CancellationToken ct);
}
