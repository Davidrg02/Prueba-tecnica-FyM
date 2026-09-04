using FyM.Users.Domain.Common;

namespace FyM.Users.Domain.Entities;

/// <summary>
/// Rol asignable a usuarios. <see cref="Level"/> es la pieza clave de
/// <see cref="Policies.UserAccessPolicy"/>: permite comparar jerarquías
/// ("¿el actor supera al objetivo?") sin hardcodear nombres de rol.
/// </summary>
public class Role : IConcurrencyAware
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int Level { get; set; }

    /// <summary>Los roles sembrados (SuperAdmin, Admin, User) no se pueden
    /// eliminar ni degradar su nivel, aunque sus permisos sí son editables.</summary>
    public bool IsSystem { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
