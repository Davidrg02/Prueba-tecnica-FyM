namespace FyM.Users.Domain.Common;

/// <summary>
/// Marca una entidad cuyos cambios de creación y modificación son
/// rastreados automáticamente por el interceptor de auditoría de EF Core.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    Guid? UpdatedBy { get; set; }
}
