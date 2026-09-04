using FyM.Users.Domain.Entities;

namespace FyM.Users.Application.Abstractions;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct);
}

public interface IDocumentTypeRepository
{
    Task<IReadOnlyList<DocumentType>> GetActiveAsync(CancellationToken ct);
}
