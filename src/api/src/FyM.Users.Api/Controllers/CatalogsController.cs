using FyM.Users.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FyM.Users.Api.Controllers;

/// <summary>Catálogos de apoyo para formularios (tipos de documento, etc.).</summary>
[ApiController]
[Route("api/v1/catalogs")]
[Produces("application/json")]
[Authorize]
public sealed class CatalogsController(ICatalogService catalogService) : ControllerBase
{
    /// <summary>Lista los tipos de documento disponibles para los perfiles.</summary>
    /// <remarks>Requiere autenticación. Devuelve el catálogo estable usado para poblar los selectores de formularios; no modifica datos.</remarks>
    [HttpGet("document-types")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<DocumentTypeDto>>> GetDocumentTypes(CancellationToken ct)
    {
        var types = await catalogService.GetDocumentTypesAsync(ct);
        return Ok(types);
    }
}
