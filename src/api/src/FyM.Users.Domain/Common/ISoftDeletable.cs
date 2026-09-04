namespace FyM.Users.Domain.Common;

/// <summary>
/// Marca una entidad que se elimina lógicamente en lugar de borrarse
/// físicamente. El filtro global de consulta de <c>AppDbContext</c>
/// excluye automáticamente las filas con <see cref="IsDeleted"/> = true.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
