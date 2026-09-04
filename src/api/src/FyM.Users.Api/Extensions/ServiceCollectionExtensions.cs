using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using FyM.Users.Api.Authorization;
using FyM.Users.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace FyM.Users.Api.Extensions;

/// <summary>Registro de los servicios transversales de la capa Api:
/// Swagger, autenticación/autorización JWT, CORS, rate limiting y health checks.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FyM Technology · API de Usuarios y Roles",
                Version = "v1",
                Description = "API REST para el registro, autenticación y administración de usuarios y roles de FyM Technology.",
                Contact = new OpenApiContact { Name = "FyM Technology" },
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese únicamente el token JWT (el prefijo 'Bearer' se agrega automáticamente).",
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    []
                },
            });

            options.EnableAnnotations();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Se lee la configuración aquí dentro (no fuera, en el cuerpo
                // del método) para que la resolución sea perezosa: WebApplicationFactory
                // (pruebas de integración) inyecta su propia configuración
                // después de que este método se registra pero antes de que
                // las opciones de JwtBearer se resuelvan por primera vez.
                var jwtSection = configuration.GetSection("Jwt");
                var key = jwtSection["Key"];
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException("Falta 'Jwt:Key' en la configuración. Defina la variable de entorno correspondiente.");
                }

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role",
                };

                options.Events = new JwtBearerEvents
                {
                    // El SecurityStamp del token se compara contra el de la
                    // base de datos: si el usuario cambió su contraseña, sus
                    // roles, o fue desactivado, el token deja de servir de
                    // inmediato aunque no haya expirado todavía.
                    OnTokenValidated = async context =>
                    {
                        var subClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        var stampClaim = context.Principal?.FindFirst("sstamp")?.Value;

                        if (!Guid.TryParse(subClaim, out var userId) || string.IsNullOrEmpty(stampClaim))
                        {
                            context.Fail("El token no contiene los claims esperados.");
                            return;
                        }

                        var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                        var user = await userRepository.GetByIdAsync(userId, includeDetails: false, context.HttpContext.RequestAborted);

                        if (user is null || !user.IsActive || !string.Equals(user.SecurityStamp.ToString(), stampClaim, StringComparison.Ordinal))
                        {
                            context.Fail("La sesión ya no es válida.");
                        }
                    },
                };
            });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    // AllowCredentials es obligatorio porque el refresh token
                    // viaja en una cookie HttpOnly; exige listar orígenes
                    // explícitos (no se puede combinar con AllowAnyOrigin).
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("auth", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueLimit = 0;
            });
        });

        return services;
    }

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'Default'.");

        services.AddHealthChecks()
            .AddSqlServer(connectionString, name: "sqlserver", tags: ["ready"]);

        return services;
    }
}
