using FyM.Users.Domain.Entities;

namespace FyM.Users.Application.Abstractions;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id, bool includePermissions, CancellationToken ct);
    Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct);
    Task<Role?> GetByNameAsync(string normalizedName, CancellationToken ct);

    /// <summary>Todos los roles junto con el número de usuarios que lo tienen
    /// asignado, resuelto en una sola consulta agregada (evita N+1).</summary>
    Task<IReadOnlyList<(Role Role, int UserCount)>> GetAllWithUserCountsAsync(CancellationToken ct);

    Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken ct, int? excludeRoleId = null);
    Task<int> CountUsersInRoleAsync(int roleId, CancellationToken ct);
    void Add(Role role);
    void Remove(Role role);
}
