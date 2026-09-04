namespace FyM.Users.Domain.Entities;

/// <summary>
/// Información demográfica y de contacto de un usuario, en relación 1:1
/// con <see cref="User"/>. Separada de la identidad de seguridad porque
/// cambia por razones de negocio distintas y porque es la parte del
/// modelo que crece (ver <c>docs/er-model.md</c> para la vía de extensión
/// hacia un futuro perfil de facturación electrónica).
/// </summary>
public class UserProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? SecondLastName { get; set; }

    public int? DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }

    public string? PhoneNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? JobTitle { get; set; }
    public string? PhotoUrl { get; set; }

    public string FullName => string.Join(' ', new[] { FirstName, MiddleName, LastName, SecondLastName }
        .Where(part => !string.IsNullOrWhiteSpace(part)));
}
