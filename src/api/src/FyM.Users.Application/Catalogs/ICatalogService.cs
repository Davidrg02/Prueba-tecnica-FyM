namespace FyM.Users.Application.Catalogs;

public sealed record DocumentTypeDto(int Id, string Code, string Name);

public interface ICatalogService
{
    Task<IReadOnlyList<DocumentTypeDto>> GetDocumentTypesAsync(CancellationToken ct);
}
