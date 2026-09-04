namespace FyM.Users.Domain.Entities;

/// <summary>
/// Permiso atómico (ej. <c>users.create</c>) que puede componerse dentro
/// de uno o varios roles. Ver <see cref="Enums.PermissionCode"/> para el
/// catálogo cerrado de códigos válidos.
/// </summary>
public class Permission
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
