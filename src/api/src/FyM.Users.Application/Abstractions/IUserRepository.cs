using FyM.Users.Application.Common;
using FyM.Users.Domain.Entities;

namespace FyM.Users.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, bool includeDetails, CancellationToken ct);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct, Guid? excludeUserId = null);
    Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct, Guid? excludeUserId = null);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserQueryParameters parameters, CancellationToken ct);

    /// <summary>Cuenta los usuarios activos, no eliminados, con rol SuperAdmin.
    /// Usado por la regla "no eliminar/desactivar al último super administrador".</summary>
    Task<int> CountActiveSuperAdminsAsync(CancellationToken ct, Guid? excludeUserId = null);

    void Add(User user);
}
