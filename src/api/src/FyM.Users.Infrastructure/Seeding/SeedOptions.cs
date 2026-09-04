namespace FyM.Users.Infrastructure.Seeding;

/// <summary>Credenciales del super administrador precreado, enlazadas
/// desde la sección <c>Seed:SuperAdmin</c> de la configuración. La
/// contraseña siempre debe llegar por variable de entorno.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public SuperAdminSeedOptions SuperAdmin { get; set; } = new();
}

public sealed class SuperAdminSeedOptions
{
    public string UserName { get; set; } = "superadmin";
    public string Email { get; set; } = "admin@fymtechnology.com";
    public string Password { get; set; } = string.Empty;
}
