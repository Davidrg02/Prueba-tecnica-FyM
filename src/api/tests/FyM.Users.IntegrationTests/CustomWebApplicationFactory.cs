using System.Data.Common;
using FyM.Users.Infrastructure.Persistence;
using FyM.Users.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FyM.Users.IntegrationTests;

/// <summary>
/// Levanta la API completa en memoria contra una base de datos SQLite
/// efímera (una conexión abierta compartida, para que persista mientras
/// dura la prueba). Sustituye a SQL Server cuando no hay Docker disponible
/// para correr las pruebas de integración, tal como documenta el plan.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection = new SqliteConnection("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-signing-key-at-least-32-bytes-long",
                ["Jwt:Issuer"] = "FyM.Users.Api.Tests",
                ["Jwt:Audience"] = "FyM.Users.Client.Tests",
                ["Seed:SuperAdmin:Email"] = "admin@fymtechnology.com",
                ["Seed:SuperAdmin:UserName"] = "superadmin",
                ["Seed:SuperAdmin:Password"] = "Adm1n#Test2026*",
                ["Database:AutoMigrate"] = "false",
                ["Cookies:Secure"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            _connection.Open();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>Crea el esquema a partir del modelo actual (no de las
    /// migraciones, que están escritas para SQL Server) y siembra los datos base.</summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
