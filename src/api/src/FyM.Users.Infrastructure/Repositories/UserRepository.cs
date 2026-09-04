using System.Linq.Expressions;
using FyM.Users.Application.Abstractions;
using FyM.Users.Application.Common;
using FyM.Users.Domain.Entities;
using FyM.Users.Domain.Enums;
using FyM.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FyM.Users.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    /// <summary>Columnas por las que el listado de usuarios puede ordenarse.
    /// Un diccionario cerrado evita construir el ORDER BY por interpolación
    /// de strings (inyección SQL / errores de nombre de columna).</summary>
    private static readonly IReadOnlyDictionary<string, Expression<Func<User, object?>>> SortExpressions =
        new Dictionary<string, Expression<Func<User, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["userName"] = u => u.UserName,
            ["email"] = u => u.Email,
            ["firstName"] = u => u.Profile != null ? u.Profile.FirstName : string.Empty,
            ["lastName"] = u => u.Profile != null ? u.Profile.LastName : string.Empty,
            ["isActive"] = u => u.IsActive,
            ["lastLoginUtc"] = u => u.LastLoginUtc,
            ["createdAtUtc"] = u => u.CreatedAtUtc,
        };

    public async Task<User?> GetByIdAsync(Guid id, bool includeDetails, CancellationToken ct)
    {
        var query = db.Users.AsQueryable();
        if (includeDetails)
        {
            query = query
                .Include(u => u.Profile).ThenInclude(p => p!.DocumentType)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission);
        }

        return await query.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        db.Users
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.UserName == userName, ct);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct, Guid? excludeUserId = null) =>
        db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && (excludeUserId == null || u.Id != excludeUserId), ct);

    public Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct, Guid? excludeUserId = null) =>
        db.Users.AnyAsync(u => u.UserName == userName && (excludeUserId == null || u.Id != excludeUserId), ct);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(UserQueryParameters parameters, CancellationToken ct)
    {
        var query = db.Users
            .Include(u => u.Profile)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = $"%{parameters.Search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.Like(u.UserName, term) ||
                EF.Functions.Like(u.Email, term) ||
                (u.Profile != null && EF.Functions.Like(u.Profile.FirstName, term)) ||
                (u.Profile != null && EF.Functions.Like(u.Profile.LastName, term)));
        }

        if (parameters.RoleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == parameters.RoleId));
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == parameters.IsActive);
        }

        var total = await query.CountAsync(ct);

        var sortExpression = parameters.SortBy is not null && SortExpressions.TryGetValue(parameters.SortBy, out var expr)
            ? expr
            : SortExpressions["createdAtUtc"];

        query = parameters.SortDescending
            ? query.OrderByDescending(sortExpression)
            : query.OrderBy(sortExpression);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<int> CountActiveSuperAdminsAsync(CancellationToken ct, Guid? excludeUserId = null) =>
        db.Users.CountAsync(u =>
            u.IsActive &&
            u.UserRoles.Any(ur => ur.Role.NormalizedName == SystemRole.SuperAdmin.ToUpperInvariant()) &&
            (excludeUserId == null || u.Id != excludeUserId), ct);

    public void Add(User user) => db.Users.Add(user);
}
