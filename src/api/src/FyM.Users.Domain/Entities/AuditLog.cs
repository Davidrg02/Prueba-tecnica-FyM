namespace FyM.Users.Domain.Entities;

/// <summary>
/// Registro de auditoría de operaciones sensibles (creación, edición,
/// eliminación de usuarios y roles). Se escribe desde el interceptor de
/// EF Core, no manualmente en cada servicio, para no olvidar ningún caso.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }

    public DateTime TimestampUtc { get; set; }
}
