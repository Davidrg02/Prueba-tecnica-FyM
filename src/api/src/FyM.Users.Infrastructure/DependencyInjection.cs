using System.Text;
using FyM.Users.Application.Abstractions;
using FyM.Users.Application.Auth;
using FyM.Users.Application.Common;
using FyM.Users.Infrastructure.Identity;
using FyM.Users.Infrastructure.Persistence;
using FyM.Users.Infrastructure.Persistence.Interceptors;
using FyM.Users.Infrastructure.Repositories;
using FyM.Users.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FyM.Users.Infrastructure;

/// <summary>Registro de todos los servicios de la capa Infrastructure:
/// EF Core, repositorios, JWT, hashing de contraseñas y el seeder.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AuditLogInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Falta la cadena de conexión 'Default' en la configuración.");

            options
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5))
                .AddInterceptors(
                    sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<AuditLogInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Key) || Encoding.UTF8.GetByteCount(options.Key) < 32)
            {
                throw new InvalidOperationException(
                    "La clave de firma JWT ('Jwt:Key') debe tener al menos 32 bytes. Configúrela mediante variable de entorno.");
            }

            return options;
        });
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AuthOptions>>().Value);

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<SeedOptions>>().Value);
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
