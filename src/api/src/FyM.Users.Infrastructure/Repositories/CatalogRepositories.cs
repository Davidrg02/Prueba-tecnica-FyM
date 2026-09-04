using FyM.Users.Application.Abstractions;
using FyM.Users.Domain.Entities;
using FyM.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FyM.Users.Infrastructure.Repositories;

public sealed class PermissionRepository(AppDbContext db) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct) =>
        await db.Permissions.AsNoTracking().OrderBy(p => p.Module).ThenBy(p => p.Code).ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct) =>
        await db.Permissions.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
}

public sealed class DocumentTypeRepository(AppDbContext db) : IDocumentTypeRepository
{
    public async Task<IReadOnlyList<DocumentType>> GetActiveAsync(CancellationToken ct) =>
        await db.DocumentTypes.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(ct);
}
