namespace FyM.Users.Domain.Entities;

/// <summary>
/// Refresh token emitido a un usuario. Solo se persiste el hash SHA-256
/// del token (nunca el valor en claro) para que una fuga de la base de
/// datos no permita reconstruir sesiones activas. La rotación encadena
/// tokens vía <see cref="ReplacedByTokenHash"/>, lo que permite detectar
/// el reuso de un token ya rotado y revocar toda la familia.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;
}
