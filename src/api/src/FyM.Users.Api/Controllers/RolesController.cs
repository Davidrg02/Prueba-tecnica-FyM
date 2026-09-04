using FyM.Users.Api.Authorization;
using FyM.Users.Application.Roles;
using FyM.Users.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FyM.Users.Api.Controllers;

/// <summary>CRUD de roles y gestión de sus permisos.</summary>
[ApiController]
[Route("api/v1/roles")]
[Produces("application/json")]
public sealed class RolesController(IRoleService roleService) : ControllerBase
{
    /// <summary>Lista todos los roles disponibles.</summary>
    /// <remarks>Requiere <c>roles.read</c>. Incluye el número de usuarios asignados y si cada rol está protegido como rol del sistema.</remarks>
    [HttpGet]
    [HasPermission(PermissionCode.RolesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RoleListItemDto>>> GetAll(CancellationToken ct)
    {
        var roles = await roleService.GetAllAsync(ct);
        return Ok(roles);
    }

    /// <summary>Obtiene el detalle de un rol por su identificador.</summary>
    /// <remarks>Requiere <c>roles.read</c>. El identificador es el entero asignado al rol.</remarks>
    [HttpGet("{id:int}")]
    [HasPermission(PermissionCode.RolesRead)]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDetailDto>> GetById(int id, CancellationToken ct)
    {
        var role = await roleService.GetByIdAsync(id, ct);
        return Ok(role);
    }

    /// <summary>Crea un rol personalizado.</summary>
    /// <remarks>Requiere <c>roles.create</c>. El nombre debe ser único. Los roles del sistema se crean mediante el seeder y no desde este endpoint.</remarks>
    [HttpPost]
    [HasPermission(PermissionCode.RolesCreate)]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDetailDto>> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var role = await roleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
    }

    /// <summary>Actualiza el nombre y la descripción de un rol.</summary>
    /// <remarks>Requiere <c>roles.update</c>. La modificación no cambia los permisos asignados; use el endpoint de permisos para ello.</remarks>
    [HttpPut("{id:int}")]
    [HasPermission(PermissionCode.RolesUpdate)]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDetailDto>> Update(int id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await roleService.UpdateAsync(id, request, ct);
        return Ok(role);
    }

    /// <summary>Elimina un rol, siempre que no sea un rol del sistema ni tenga usuarios asignados.</summary>
    /// <remarks>Requiere <c>roles.delete</c>. La operación falla si el rol está protegido o todavía tiene usuarios asignados.</remarks>
    [HttpDelete("{id:int}")]
    [HasPermission(PermissionCode.RolesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await roleService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Reemplaza el conjunto completo de permisos de un rol.</summary>
    /// <remarks>Requiere <c>roles.manage-permissions</c>. La lista enviada sustituye todos los permisos actuales del rol.</remarks>
    [HttpPut("{id:int}/permissions")]
    [HasPermission(PermissionCode.RolesManagePermissions)]
    [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDetailDto>> UpdatePermissions(int id, UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        var role = await roleService.UpdatePermissionsAsync(id, request, ct);
        return Ok(role);
    }
}
