namespace FyM.Users.Domain.Enums;

/// <summary>
/// Códigos de permiso sembrados por el <c>DatabaseSeeder</c> y usados como
/// claims <c>permission</c> dentro del JWT. Los controladores se protegen
/// con <c>[HasPermission(PermissionCode.UsersCreate)]</c> en lugar de
/// comprobar el nombre del rol, para que crear un rol nuevo sea un INSERT
/// y no un despliegue.
/// </summary>
public static class PermissionCode
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string UsersAssignRoles = "users.assign-roles";
    public const string UsersResetPassword = "users.reset-password";

    public const string RolesRead = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string RolesManagePermissions = "roles.manage-permissions";

    public const string AuditRead = "audit.read";

    public const string ProfileReadOwn = "profile.read-own";
    public const string ProfileUpdateOwn = "profile.update-own";

    /// <summary>Todos los códigos declarados, usado por el seeder para poblar
    /// la tabla <c>Permissions</c> sin duplicar la lista en otro lugar.</summary>
    public static readonly IReadOnlyList<(string Code, string Module, string Description)> All =
    [
        (UsersRead, "Users", "Consultar usuarios"),
        (UsersCreate, "Users", "Crear usuarios"),
        (UsersUpdate, "Users", "Editar usuarios"),
        (UsersDelete, "Users", "Eliminar (desactivar) usuarios"),
        (UsersAssignRoles, "Users", "Asignar roles a usuarios"),
        (UsersResetPassword, "Users", "Restablecer contraseña de otro usuario"),
        (RolesRead, "Roles", "Consultar roles"),
        (RolesCreate, "Roles", "Crear roles"),
        (RolesUpdate, "Roles", "Editar roles"),
        (RolesDelete, "Roles", "Eliminar roles"),
        (RolesManagePermissions, "Roles", "Gestionar permisos de un rol"),
        (AuditRead, "Audit", "Consultar el registro de auditoría"),
        (ProfileReadOwn, "Profile", "Consultar el propio perfil"),
        (ProfileUpdateOwn, "Profile", "Editar el propio perfil"),
    ];
}
