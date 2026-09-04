using FyM.Users.Domain.Common;

namespace FyM.Users.Domain.Entities;

/// <summary>
/// Identidad de seguridad de un usuario del sistema. Deliberadamente no
/// contiene datos demográficos (ver <see cref="UserProfile"/>): las
/// consultas de autenticación solo tocan esta tabla, sin cargar
/// información personal innecesaria.
/// </summary>
public class User : IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Se rota en cada cambio de contraseña o de roles para que
    /// los access tokens ya emitidos dejen de ser válidos de inmediato,
    /// sin depender de su expiración natural.</summary>
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }

    /// <summary>Marca cuentas precreadas (el super administrador y los
    /// demás usuarios sembrados) que no pueden eliminarse físicamente.</summary>
    public bool IsSystem { get; set; }

    public int AccessFailedCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }

    public UserProfile? Profile { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>Concurrencia optimista: una escritura sobre una fila
    /// modificada entretanto devuelve 409 en lugar de pisar el cambio.</summary>
    public byte[] RowVersion { get; set; } = [];

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc.HasValue && LockoutEndUtc.Value > nowUtc;
}
