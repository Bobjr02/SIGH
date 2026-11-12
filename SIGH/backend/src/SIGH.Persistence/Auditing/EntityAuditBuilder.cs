using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SIGH.Persistence.Auditing;

public static class AuditActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string SoftDeleted = "SoftDeleted";
}

/// <summary>Gera registros de AuditLog a partir do ChangeTracker, seguindo <see cref="AuditPolicy"/>.</summary>
public static class EntityAuditBuilder
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    public static List<AuditLog> Build(
        ChangeTracker changeTracker,
        DateTimeOffset now,
        Guid? userId,
        string? ipAddress,
        string? userAgent,
        string? correlationId)
    {
        var logs = new List<AuditLog>();

        foreach (var entry in changeTracker.Entries())
        {
            if (!AuditPolicy.IsAudited(entry.Metadata.ClrType))
                continue;

            var action = ResolveAction(entry);
            if (action is null)
                continue;

            var (oldValues, newValues) = CollectChanges(entry, action);
            if (action == AuditActions.Updated && newValues.Count == 0)
                continue;

            logs.Add(new AuditLog
            {
                Action = action,
                Entity = entry.Metadata.ClrType.Name,
                EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString(),
                OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues, JsonOptions) : null,
                NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues, JsonOptions) : null,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CorrelationId = correlationId,
                CreatedAt = now,
                CreatedBy = userId
            });
        }

        return logs;
    }

    private static string? ResolveAction(EntityEntry entry) => entry.State switch
    {
        EntityState.Added => AuditActions.Created,
        EntityState.Deleted => AuditActions.Deleted,
        EntityState.Modified when IsBeingSoftDeleted(entry) => AuditActions.SoftDeleted,
        EntityState.Modified => AuditActions.Updated,
        _ => null
    };

    private static bool IsBeingSoftDeleted(EntityEntry entry)
    {
        var isDeleted = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
        return isDeleted is { IsModified: true, CurrentValue: true } && isDeleted.OriginalValue is false;
    }

    private static (Dictionary<string, object?> Old, Dictionary<string, object?> New) CollectChanges(EntityEntry entry, string action)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (property.Metadata.IsPrimaryKey() || AuditPolicy.IgnoredProperties.Contains(name))
                continue;

            var sensitive = AuditPolicy.SensitiveProperties.Contains(name);

            switch (action)
            {
                case AuditActions.Created:
                    if (property.CurrentValue is null) continue;
                    newValues[name] = sensitive ? AuditPolicy.Redacted : property.CurrentValue;
                    break;

                case AuditActions.Deleted:
                    oldValues[name] = sensitive ? AuditPolicy.Redacted : property.OriginalValue;
                    break;

                default:
                    if (!property.IsModified || Equals(property.OriginalValue, property.CurrentValue))
                        continue;
                    oldValues[name] = sensitive ? AuditPolicy.Redacted : property.OriginalValue;
                    newValues[name] = sensitive ? AuditPolicy.Redacted : property.CurrentValue;
                    break;
            }
        }

        return (oldValues, newValues);
    }
}
