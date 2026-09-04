using FyM.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FyM.Users.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core del sistema de usuarios y roles. Las convenciones
/// de mapeo (claves, índices, filtros) viven en las clases
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> de <c>Persistence/Configurations</c>,
/// no como atributos sobre las entidades.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
