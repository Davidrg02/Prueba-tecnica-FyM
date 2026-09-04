using FyM.Users.Application.Abstractions;
using FyM.Users.Domain.Entities;
using FyM.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FyM.Users.Infrastructure.Repositories;

public sealed class RoleRepository(AppDbContext db) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(int id, bool includePermissions, CancellationToken ct)
    {
        var query = db.Roles.AsQueryable();
        if (includePermissions)
        {
            query = query.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission);
        }

        return await query.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct) =>
        await db.Roles.Where(r => ids.Contains(r.Id)).ToListAsync(ct);

    public Task<Role?> GetByNameAsync(string normalizedName, CancellationToken ct) =>
        db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, ct);

    public async Task<IReadOnlyList<(Role Role, int UserCount)>> GetAllWithUserCountsAsync(CancellationToken ct)
    {
        var result = await db.Roles
            .AsNoTracking()
            .OrderByDescending(r => r.Level)
            .Select(r => new { Role = r, UserCount = r.UserRoles.Count })
            .ToListAsync(ct);

        return result.Select(x => (x.Role, x.UserCount)).ToList();
    }

    public Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken ct, int? excludeRoleId = null) =>
        db.Roles.AnyAsync(r => r.NormalizedName == normalizedName && (excludeRoleId == null || r.Id != excludeRoleId), ct);

    public Task<int> CountUsersInRoleAsync(int roleId, CancellationToken ct) =>
        db.UserRoles.CountAsync(ur => ur.RoleId == roleId, ct);

    public void Add(Role role) => db.Roles.Add(role);

    public void Remove(Role role) => db.Roles.Remove(role);
}
