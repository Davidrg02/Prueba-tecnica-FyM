namespace FyM.Users.Domain.Entities;

/// <summary>Tabla de unión N:M entre <see cref="User"/> y <see cref="Role"/>.</summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; }
    public Guid? AssignedByUserId { get; set; }
}
