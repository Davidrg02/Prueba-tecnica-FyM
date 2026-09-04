using FyM.Users.Api.Authorization;
using FyM.Users.Application.Permissions;
using FyM.Users.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FyM.Users.Api.Controllers;

/// <summary>Catálogo de permisos disponibles, agrupados por módulo.</summary>
[ApiController]
[Route("api/v1/permissions")]
[Produces("application/json")]
public sealed class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    /// <summary>Obtiene el catálogo completo de permisos agrupado por módulo.</summary>
    /// <remarks>Requiere <c>roles.read</c>. El catálogo está pensado para construir la pantalla de administración de permisos y sus elementos son de solo lectura.</remarks>
    [HttpGet]
    [HasPermission(PermissionCode.RolesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PermissionGroupDto>>> GetGrouped(CancellationToken ct)
    {
        var groups = await permissionService.GetGroupedAsync(ct);
        return Ok(groups);
    }
}
