namespace FyM.Users.Domain.Entities;

/// <summary>Catálogo de tipos de documento de identidad (CC, CE, TI, NIT, PAS).</summary>
public class DocumentType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
