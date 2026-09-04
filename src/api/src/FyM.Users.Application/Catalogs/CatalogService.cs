using FyM.Users.Application.Abstractions;

namespace FyM.Users.Application.Catalogs;

public sealed class CatalogService(IDocumentTypeRepository documentTypeRepository) : ICatalogService
{
    public async Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken ct)
    {
        var types = await documentTypeRepository.GetActiveAsync(ct);
        return types.Select(t => new DocumentTypeDto(t.Id, t.Code, t.Name)).ToList();
    }
}
