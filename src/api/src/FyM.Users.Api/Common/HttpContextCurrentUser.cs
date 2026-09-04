using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FyM.Users.Application.Common;
using FyM.Users.Domain.Enums;

namespace FyM.Users.Api.Common;

/// <summary>
/// Lee la identidad del usuario autenticado directamente de los claims del
/// JWT ya validado en <c>HttpContext.User</c>, sin volver a consultar la
/// base de datos en cada request. Fuera de una petición HTTP (por ejemplo,
/// durante el seeding en el arranque) se comporta como usuario anónimo.
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : Guid.Empty;

    public string Email => Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll("role").Select(c => c.Value).ToList() ?? [];

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).ToList() ?? [];

    public int MaxRoleLevel => int.TryParse(Principal?.FindFirst("rlevel")?.Value, out var level) ? level : 0;

    public bool IsSuperAdmin => Roles.Contains(SystemRole.SuperAdmin);

    public bool HasPermission(string permissionCode) => Permissions.Contains(permissionCode);
}
