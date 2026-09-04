namespace FyM.Users.Infrastructure.Identity;

/// <summary>Configuración de emisión de JWT, enlazada desde la sección
/// <c>Jwt</c> de <c>appsettings.json</c>. La clave de firma siempre debe
/// llegar por variable de entorno, nunca committeada en texto plano.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
}
