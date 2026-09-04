using FyM.Users.Api.Authorization;
using FyM.Users.Application.Common;
using FyM.Users.Application.Users;
using FyM.Users.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FyM.Users.Api.Controllers;

/// <summary>CRUD de usuarios y gestión de su propio perfil.</summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Lista paginada de usuarios, con búsqueda, filtro por rol/estado y orden.</summary>
    /// <remarks>Requiere <c>users.read</c>. Los parámetros de paginación y filtrado se envían en la query string y la respuesta contiene los elementos y los metadatos de paginación.</remarks>
    [HttpGet]
    [HasPermission(PermissionCode.UsersRead)]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetPaged([FromQuery] UserQueryParameters parameters, CancellationToken ct)
    {
        var result = await userService.GetPagedAsync(parameters, ct);
        return Ok(result);
    }

    /// <summary>Obtiene el detalle de un usuario por su identificador.</summary>
    /// <remarks>Requiere <c>users.read</c>. El identificador debe ser un GUID.</remarks>
    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCode.UsersRead)]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(id, ct);
        return Ok(user);
    }

    /// <summary>Crea un usuario nuevo con perfil y roles iniciales.</summary>
    /// <remarks>Requiere <c>users.create</c>. La contraseña se almacena con hash y nunca se devuelve. La respuesta incluye la ubicación del recurso creado.</remarks>
    [HttpPost]
    [HasPermission(PermissionCode.UsersCreate)]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Actualiza los datos de contacto y perfil de un usuario existente.</summary>
    /// <remarks>Requiere <c>users.update</c>. No modifica la contraseña ni los roles; esas operaciones tienen endpoints específicos.</remarks>
    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCode.UsersUpdate)]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> Update(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await userService.UpdateAsync(id, request, ct);
        return Ok(user);
    }

    /// <summary>Activa o desactiva la cuenta de un usuario.</summary>
    /// <remarks>Requiere <c>users.update</c>. Una cuenta desactivada no puede iniciar sesión y sus JWT existentes dejan de ser válidos.</remarks>
    [HttpPatch("{id:guid}/status")]
    [HasPermission(PermissionCode.UsersUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, UpdateUserStatusRequest request, CancellationToken ct)
    {
        await userService.SetStatusAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Elimina lógicamente un usuario.</summary>
    /// <remarks>Requiere <c>users.delete</c>. El registro se conserva para mantener la trazabilidad, pero deja de estar disponible como usuario activo.</remarks>
    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCode.UsersDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await userService.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Reemplaza el conjunto completo de roles de un usuario.</summary>
    /// <remarks>Requiere <c>users.assign-roles</c>. La lista enviada sustituye todos los roles actuales y la respuesta devuelve el usuario actualizado.</remarks>
    [HttpPut("{id:guid}/roles")]
    [HasPermission(PermissionCode.UsersAssignRoles)]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailDto>> AssignRoles(Guid id, AssignRolesRequest request, CancellationToken ct)
    {
        var user = await userService.AssignRolesAsync(id, request, ct);
        return Ok(user);
    }

    /// <summary>Restablece la contraseña de otro usuario y le exige cambiarla en el próximo ingreso.</summary>
    /// <remarks>Requiere <c>users.reset-password</c>. Revoca las sesiones activas del usuario objetivo y marca la cuenta para cambio de contraseña.</remarks>
    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(PermissionCode.UsersResetPassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request, CancellationToken ct)
    {
        await userService.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Perfil del usuario autenticado.</summary>
    /// <remarks>Requiere <c>profile.read-own</c>. El usuario se determina mediante el JWT y no mediante un id proporcionado por el cliente.</remarks>
    [HttpGet("me/profile")]
    [HasPermission(PermissionCode.ProfileReadOwn)]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailDto>> GetOwnProfile(CancellationToken ct)
    {
        var profile = await userService.GetOwnProfileAsync(ct);
        return Ok(profile);
    }

    /// <summary>Actualiza el perfil del usuario autenticado (no incluye credenciales ni roles).</summary>
    /// <remarks>Requiere <c>profile.update-own</c>. Solo actualiza datos personales permitidos; para cambiar la contraseña use <c>POST /api/v1/auth/change-password</c>.</remarks>
    [HttpPut("me/profile")]
    [HasPermission(PermissionCode.ProfileUpdateOwn)]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailDto>> UpdateOwnProfile(UpdateOwnProfileRequest request, CancellationToken ct)
    {
        var profile = await userService.UpdateOwnProfileAsync(request, ct);
        return Ok(profile);
    }
}
