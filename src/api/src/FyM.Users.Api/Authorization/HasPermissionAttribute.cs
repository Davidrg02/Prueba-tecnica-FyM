using Microsoft.AspNetCore.Authorization;

namespace FyM.Users.Api.Authorization;

/// <summary>
/// Protege un endpoint exigiendo un permiso concreto (ej. <c>users.create</c>)
/// en lugar de un nombre de rol quemado en el código: crear un rol nuevo con
/// esos permisos es un INSERT en la base de datos, no un despliegue.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class HasPermissionAttribute(string permissionCode) : AuthorizeAttribute(BuildPolicyName(permissionCode))
{
    public const string PolicyPrefix = "Permission:";

    public static string BuildPolicyName(string permissionCode) => $"{PolicyPrefix}{permissionCode}";
}
