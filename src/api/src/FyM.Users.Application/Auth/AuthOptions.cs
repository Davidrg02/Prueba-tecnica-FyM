namespace FyM.Users.Application.Auth;

/// <summary>Configuración del módulo de autenticación, enlazada desde
/// la sección <c>Auth</c> de <c>appsettings.json</c> por la capa Api.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool AllowSelfRegistration { get; set; } = true;
}
