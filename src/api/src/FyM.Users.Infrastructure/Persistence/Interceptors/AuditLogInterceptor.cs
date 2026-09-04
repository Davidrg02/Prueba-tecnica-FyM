using System.Text.Json;
using FyM.Users.Application.Common;
using FyM.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FyM.Users.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Escribe un <see cref="AuditLog"/> por cada alta, edición o baja de
/// <see cref="User"/> o <see cref="Role"/> — las dos entidades sensibles
/// del sistema — capturado desde el propio <c>ChangeTracker</c> para que
/// ningún caso de uso pueda olvidarse de auditar una operación.
/// Los campos secretos (hash de contraseña, security stamp, row version)
/// nunca se serializan, ni siquiera hasheados.
/// </summary>
public sealed class AuditLogInterceptor(ICurrentUser currentUser, IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    private static readonly Type[] AuditedTypes = [typeof(User), typeof(Role)];
    private static readonly string[] ExcludedProperties = ["PasswordHash", "SecurityStamp", "RowVersion"];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CaptureAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CaptureAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureAuditLogs(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = dateTimeProvider.UtcNow;
        Guid? userId = currentUser.IsAuthenticated ? currentUser.UserId : null;

        var entries = context.ChangeTracker.Entries()
            .Where(e => AuditedTypes.Contains(e.Entity.GetType())
                && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            context.Set<AuditLog>().Add(new AuditLog
            {
                UserId = userId,
                Action = entry.State.ToString(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetEntityId(entry),
                OldValues = entry.State == EntityState.Added ? null : Serialize(entry.OriginalValues),
                NewValues = entry.State == EntityState.Deleted ? null : Serialize(entry.CurrentValues),
                TimestampUtc = now,
            });
        }
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return string.Empty;
        }

        var values = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty);
        return string.Join(',', values);
    }

    private static string Serialize(PropertyValues values)
    {
        var dict = values.Properties
            .Where(p => !ExcludedProperties.Contains(p.Name))
            .ToDictionary(p => p.Name, p => values[p]);
        return JsonSerializer.Serialize(dict);
    }
}
