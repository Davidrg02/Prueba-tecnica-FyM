using FyM.Users.Api.Common;
using FyM.Users.Api.Extensions;
using FyM.Users.Api.Filters;
using FyM.Users.Api.Middleware;
using FyM.Users.Application;
using FyM.Users.Application.Common;
using FyM.Users.Infrastructure;
using FyM.Users.Infrastructure.Identity;
using FyM.Users.Infrastructure.Persistence;
using FyM.Users.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FyM.Users.Api"));

builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerDocumentation();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddRateLimitingPolicies();
builder.Services.AddAppHealthChecks(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// Falla rápido en el arranque si la clave JWT no cumple la política mínima,
// en lugar de fallar de forma confusa en el primer login.
app.Services.GetRequiredService<JwtOptions>();

if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    await ApplyMigrationsAndSeedAsync(app);
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    await next();
});

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FyM Technology · API de Usuarios y Roles v1");
    options.DisplayRequestDuration();
});

app.UseCors("Default");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// /health es una sonda de vida pura (no toca la base de datos, para no
// depender de SQL Server solo para saber si el proceso sigue vivo);
// /health/ready sí evalúa la conexión real, para el healthcheck de Docker Compose.
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

// Aplica migraciones pendientes (con reintentos, porque SQL Server puede
// tardar en aceptar conexiones aunque su healthcheck ya pase) y ejecuta el seeder idempotente.
static async Task ApplyMigrationsAndSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 10;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            logger.LogWarning(ex, "No se pudo migrar la base de datos (intento {Attempt}/{MaxRetries}). Reintentando en 5s...", attempt, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Punto de entrada expuesto para WebApplicationFactory en las pruebas de integración.
public partial class Program;
