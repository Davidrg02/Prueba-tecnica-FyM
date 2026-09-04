using FyM.Users.Application.Abstractions;
using FyM.Users.Domain.Entities;
using FyM.Users.Domain.Enums;
using FyM.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FyM.Users.Infrastructure.Seeding;

/// <summary>
/// Siembra el catálogo de tipos de documento, el catálogo cerrado de
/// permisos, los tres roles del sistema (SuperAdmin, Admin, User) y el
/// usuario super administrador precreado que exige el enunciado. Cada
/// paso es idempotente: ejecutar el seeder varias veces no duplica filas
/// ni sobrescribe la contraseña de un super administrador ya existente.
/// </summary>
public sealed class DatabaseSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    SeedOptions seedOptions,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedDocumentTypesAsync(ct);
        await SeedPermissionsAsync(ct);
        var roles = await SeedRolesAsync(ct);
        await SeedSuperAdminAsync(roles, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedDocumentTypesAsync(CancellationToken ct)
    {
        if (await db.DocumentTypes.AnyAsync(ct))
        {
            return;
        }

        db.DocumentTypes.AddRange(
            new DocumentType { Code = "CC", Name = "Cédula de ciudadanía" },
            new DocumentType { Code = "CE", Name = "Cédula de extranjería" },
            new DocumentType { Code = "TI", Name = "Tarjeta de identidad" },
            new DocumentType { Code = "NIT", Name = "Número de identificación tributaria" },
            new DocumentType { Code = "PAS", Name = "Pasaporte" });
    }

    private async Task SeedPermissionsAsync(CancellationToken ct)
    {
        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        var missing = PermissionCode.All.Where(p => !existingCodes.Contains(p.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        db.Permissions.AddRange(missing.Select(p => new Permission { Code = p.Code, Module = p.Module, Description = p.Description }));
        await db.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken ct)
    {
        var permissionsByCode = await db.Permissions.ToDictionaryAsync(p => p.Code, ct);

        (string Name, int Level, string[] PermissionCodes)[] definitions =
        [
            (SystemRole.SuperAdmin, SystemRole.Level.SuperAdmin, [.. PermissionCode.All.Select(p => p.Code)]),
            (SystemRole.Admin, SystemRole.Level.Admin,
                [.. PermissionCode.All.Select(p => p.Code).Except([PermissionCode.RolesDelete, PermissionCode.AuditRead])]),
            (SystemRole.User, SystemRole.Level.User, [PermissionCode.ProfileReadOwn, PermissionCode.ProfileUpdateOwn]),
        ];

        var roles = new Dictionary<string, Role>();

        foreach (var (name, level, permissionCodes) in definitions)
        {
            var normalizedName = name.ToUpperInvariant();
            var role = await db.Roles
                .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, ct);

            if (role is null)
            {
                role = new Role
                {
                    Name = name,
                    NormalizedName = normalizedName,
                    Description = $"Rol {name} sembrado por el sistema.",
                    Level = level,
                    IsSystem = true,
                };
                db.Roles.Add(role);
            }

            var currentCodes = role.RolePermissions.Select(rp => rp.Permission.Code).ToHashSet();
            foreach (var code in permissionCodes.Where(c => !currentCodes.Contains(c) && permissionsByCode.ContainsKey(c)))
            {
                role.RolePermissions.Add(new RolePermission { Role = role, Permission = permissionsByCode[code] });
            }

            roles[name] = role;
        }

        await db.SaveChangesAsync(ct);
        return roles;
    }

    private async Task SeedSuperAdminAsync(Dictionary<string, Role> roles, CancellationToken ct)
    {
        var normalizedEmail = seedOptions.SuperAdmin.Email.Trim().ToUpperInvariant();
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(seedOptions.SuperAdmin.Password))
        {
            logger.LogWarning("No se sembró el super administrador: falta configurar Seed:SuperAdmin:Password.");
            return;
        }

        var superAdminRole = roles[SystemRole.SuperAdmin];
        var user = new User
        {
            UserName = seedOptions.SuperAdmin.UserName,
            Email = seedOptions.SuperAdmin.Email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(seedOptions.SuperAdmin.Password),
            IsSystem = true,
            IsActive = true,
            Profile = new UserProfile { FirstName = "Super", LastName = "Administrador" },
        };
        user.UserRoles.Add(new UserRole { User = user, Role = superAdminRole, AssignedAtUtc = DateTime.UtcNow });

        db.Users.Add(user);
        logger.LogInformation("Super administrador sembrado con correo {Email}.", seedOptions.SuperAdmin.Email);
    }
}
