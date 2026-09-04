namespace FyM.Users.Application.Common;

/// <summary>
/// Abstrae el acceso al usuario autenticado de la petición HTTP actual.
/// La implementación (en Infrastructure/Api) lee los claims del
/// <c>HttpContext</c>; los servicios de aplicación nunca tocan
/// <c>IHttpContextAccessor</c> directamente.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    int MaxRoleLevel { get; }
    bool IsSuperAdmin { get; }
    bool HasPermission(string permissionCode);
}
