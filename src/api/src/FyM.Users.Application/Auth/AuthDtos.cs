namespace FyM.Users.Application.Auth;

public sealed record RegisterRequest(
    string UserName,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    int? DocumentTypeId,
    string? DocumentNumber,
    string? PhoneNumber);

public sealed record LoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AuthenticatedUserDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool MustChangePassword);

/// <summary>Resultado de login/registro/refresh. El controlador extrae
/// <see cref="RefreshToken"/> para ponerlo en la cookie HttpOnly y nunca
/// lo devuelve dentro del cuerpo JSON de la respuesta.</summary>
public sealed record LoginResult(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    AuthenticatedUserDto User);
