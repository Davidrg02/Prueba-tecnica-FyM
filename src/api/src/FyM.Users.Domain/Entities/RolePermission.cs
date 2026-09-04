namespace FyM.Users.Domain.Entities;

/// <summary>Tabla de unión N:M entre <see cref="Role"/> y <see cref="Permission"/>.</summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
