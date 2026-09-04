namespace FyM.Users.Domain.Common;

/// <summary>
/// Marca una entidad con concurrencia optimista. El token se genera del
/// lado del cliente (interceptor de EF Core) en lugar de delegarlo al tipo
/// nativo <c>rowversion</c> de SQL Server, para que el mismo modelo funcione
/// igual contra cualquier proveedor relacional (incluida la base SQLite que
/// usan las pruebas de integración).
/// </summary>
public interface IConcurrencyAware
{
    byte[] RowVersion { get; set; }
}
