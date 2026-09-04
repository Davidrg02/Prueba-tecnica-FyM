using FyM.Users.Domain.Exceptions;

namespace FyM.Users.Domain.Policies;

/// <summary>Datos mínimos del actor autenticado necesarios para evaluar las reglas de acceso.</summary>
public readonly record struct ActorContext(Guid UserId, int MaxRoleLevel, bool IsSuperAdmin);

/// <summary>Datos mínimos del usuario objetivo de una operación sobre usuarios.</summary>
public readonly record struct TargetUserContext(Guid UserId, int MaxRoleLevel, bool IsSystem);

/// <summary>
/// Reglas de negocio transversales sobre quién puede operar sobre quién.
/// Centralizarlas aquí evita reimplementar "Admin no puede tocar a
/// SuperAdmin" con un <c>if</c> distinto en cada endpoint: la jerarquía se
/// expresa una sola vez comparando <see cref="Entities.Role.Level"/>.
/// Todas las violaciones lanzan <see cref="ForbiddenOperationException"/>
/// (403), nunca dejan pasar la operación silenciosamente.
/// </summary>
public sealed class UserAccessPolicy
{
    /// <summary>Regla 1: un actor no puede operar sobre un usuario cuyo
    /// nivel máximo de rol sea igual o superior al suyo, salvo sobre sí
    /// mismo. El super administrador no está sujeto a esta jerarquía (de
    /// lo contrario ningún super administrador podría operar sobre otro,
    /// y la regla 3 —proteger al último super administrador activo—
    /// nunca sería alcanzable).</summary>
    public void EnsureCanModify(ActorContext actor, TargetUserContext target)
    {
        if (actor.UserId == target.UserId || actor.IsSuperAdmin)
        {
            return;
        }

        if (target.MaxRoleLevel >= actor.MaxRoleLevel)
        {
            throw new ForbiddenOperationException(
                "No tiene permisos para operar sobre un usuario con un nivel de rol igual o superior al suyo.");
        }
    }

    /// <summary>Reglas 1, 2, 3 y 4 aplicadas a desactivar o eliminar un usuario.</summary>
    public void EnsureCanDeactivateOrDelete(ActorContext actor, TargetUserContext target, bool isLastActiveSuperAdmin)
    {
        if (actor.UserId == target.UserId)
        {
            throw new ForbiddenOperationException("No puede eliminar ni desactivar su propia cuenta.");
        }

        EnsureCanModify(actor, target);

        if (target.IsSystem)
        {
            throw new ForbiddenOperationException("Los usuarios del sistema no pueden eliminarse ni desactivarse.");
        }

        if (isLastActiveSuperAdmin)
        {
            throw new ForbiddenOperationException("No puede eliminar ni desactivar al único super administrador activo del sistema.");
        }
    }

    /// <summary>Reglas 1 y 5 aplicadas a la reasignación de roles de un usuario.
    /// <paramref name="maxRequestedRoleLevel"/> es el nivel más alto entre los
    /// roles que se le quieren asignar al usuario objetivo.</summary>
    public void EnsureCanAssignRoles(ActorContext actor, TargetUserContext target, int maxRequestedRoleLevel)
    {
        EnsureCanModify(actor, target);
        EnsureCanGrantRoleLevel(actor, maxRequestedRoleLevel);
    }

    /// <summary>Regla 5 generalizada: sin importar si ya existe un usuario
    /// objetivo (aplica también a la creación de usuarios nuevos), un actor
    /// no puede otorgar un rol de nivel igual o superior al suyo, salvo que
    /// sea super administrador — el único que puede crear otro super
    /// administrador o delegar roles de cualquier nivel.</summary>
    public void EnsureCanGrantRoleLevel(ActorContext actor, int maxRequestedRoleLevel)
    {
        if (!actor.IsSuperAdmin && maxRequestedRoleLevel >= actor.MaxRoleLevel)
        {
            throw new ForbiddenOperationException("No puede asignar un rol con un nivel igual o superior al suyo.");
        }
    }

    /// <summary>Regla 1 aplicada a restablecer la contraseña de otro usuario.</summary>
    public void EnsureCanResetPassword(ActorContext actor, TargetUserContext target) =>
        EnsureCanModify(actor, target);

    /// <summary>Regla 4 aplicada a roles: los roles del sistema no se eliminan
    /// ni se les cambia el nombre o el nivel (sus permisos sí son editables).</summary>
    public void EnsureRoleIsMutable(bool roleIsSystem, string action)
    {
        if (roleIsSystem)
        {
            throw new ForbiddenOperationException($"El rol es un rol del sistema y no puede {action}.");
        }
    }

    /// <summary>Ningún actor puede crear ni degradar un rol a un nivel igual o
    /// superior al suyo: evita que un Admin fabrique un rol que lo supere.</summary>
    public void EnsureCanSetRoleLevel(ActorContext actor, int requestedLevel)
    {
        if (requestedLevel >= actor.MaxRoleLevel)
        {
            throw new ForbiddenOperationException("No puede crear ni modificar un rol con un nivel igual o superior al suyo.");
        }
    }
}
