using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Xams.Core.Base;

namespace Xams.Core.Utils;

/// <summary>
/// Stages a change set on the DbContext without loading entities, then saves it with one SaveChanges.
/// </summary>
internal static class AggregateWriter
{
    internal static async Task SaveAsync(IXamsDbContext db, AggregateChangeSet changes, Guid userId, DateTime now)
    {
        var errors = new List<AggregateError>();
        var tracker = db.ChangeTracker;
        var detectChanges = tracker.AutoDetectChangesEnabled;
        tracker.AutoDetectChangesEnabled = false;
        try
        {
            // Entities the caller already tracks are reused instead of attaching a second instance
            var tracked = new Dictionary<(Type, object), EntityEntry>();
            foreach (var entry in tracker.Entries())
            {
                var key = entry.Metadata.FindPrimaryKey();
                if (key is { Properties.Count: 1 } && entry.Property(key.Properties[0].Name).CurrentValue is { } id)
                {
                    tracked[(entry.Metadata.GetRootType().ClrType, id)] = entry;
                }
            }

            EntityEntry? Tracked(Type type, Guid id) =>
                tracked.GetValueOrDefault((db.Model.FindEntityType(type)!.GetRootType().ClrType, id));

            foreach (var create in changes.Creates)
            {
                if (Tracked(create.Entity.GetType(), create.Id) != null)
                {
                    errors.Add(Clash(create.TableName, create.Id, "is created, but the DbContext already tracks it"));
                    continue;
                }

                db.Entry(create.Entity).State = EntityState.Added;
            }

            // Updates before deletes: a child moved away from a deleted parent must not be cascaded with it
            foreach (var update in changes.Updates.Where(x => x.Changes.Count > 0))
            {
                var entry = Tracked(update.EntityType, update.Id);
                if (entry?.State is EntityState.Added or EntityState.Deleted)
                {
                    errors.Add(Clash(update.TableName, update.Id,
                        $"is updated, but the DbContext already tracks it as {entry.State}"));
                    continue;
                }

                entry ??= Attach(db, update.EntityType, update.Id, update.CurrentValues, EntityState.Unchanged);
                foreach (var (field, change) in update.Changes)
                {
                    Write(entry, field, change.New);
                }

                if (entry.Metadata.FindProperty("UpdatedDate") != null)
                {
                    Write(entry, "UpdatedDate", now);
                }

                if (entry.Metadata.FindProperty("UpdatedById") != null)
                {
                    Write(entry, "UpdatedById", userId);
                }
            }

            foreach (var delete in changes.Deletes)
            {
                var entry = Tracked(delete.EntityType, delete.Id);
                if (entry?.State == EntityState.Added)
                {
                    errors.Add(Clash(delete.TableName, delete.Id, "is deleted, but the DbContext is adding it"));
                    continue;
                }

                if (entry == null)
                {
                    Attach(db, delete.EntityType, delete.Id, delete.CurrentValues, EntityState.Deleted);
                }
                else if (entry.State != EntityState.Deleted)
                {
                    entry.State = EntityState.Deleted;
                }
            }
        }
        finally
        {
            tracker.AutoDetectChangesEnabled = detectChanges;
        }

        if (errors.Count > 0)
        {
            throw new AggregateUpdateException(errors);
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            throw new AggregateUpdateException(DatabaseErrors(e), e);
        }
    }

    // An instance holding the key and the values already read, so EF knows the original foreign keys
    private static EntityEntry Attach(IXamsDbContext db, Type type, Guid id, Dictionary<string, object?> values,
        EntityState state)
    {
        var entity = Activator.CreateInstance(type)
                     ?? throw new InvalidOperationException($"Couldn't create a {type.Name}.");
        var entityType = db.Model.FindEntityType(type)!;
        foreach (var property in entityType.GetProperties())
        {
            if (property.PropertyInfo == null || !values.TryGetValue(property.Name, out var value))
            {
                continue;
            }

            if (value == null
                    ? !property.ClrType.IsValueType || Nullable.GetUnderlyingType(property.ClrType) != null
                    : property.ClrType.IsInstanceOfType(value) ||
                      Nullable.GetUnderlyingType(property.ClrType)?.IsInstanceOfType(value) == true)
            {
                property.PropertyInfo.SetValue(entity, value);
            }
        }

        entityType.FindPrimaryKey()!.Properties[0].PropertyInfo!.SetValue(entity, id);

        // Setting the state attaches only this entity, not any navigation the constructor initialized
        var entry = db.Entry(entity);
        entry.State = state;
        return entry;
    }

    private static void Write(EntityEntry entry, string field, object? value)
    {
        var property = entry.Property(field);
        property.CurrentValue = value;
        property.IsModified = true;
    }

    private static AggregateError Clash(string table, Guid id, string problem) =>
        new(AggregateErrorKind.Invalid, table, id, null, $"{table} {id} {problem}.");

    private static List<AggregateError> DatabaseErrors(DbUpdateException exception)
    {
        var kind = exception is DbUpdateConcurrencyException ? AggregateErrorKind.Conflict : AggregateErrorKind.Database;
        var reason = exception is DbUpdateConcurrencyException
            ? "was changed or deleted by someone else during the save"
            : $"was rejected by the database: {exception.InnerException?.Message ?? exception.Message}";

        var errors = new List<AggregateError>();
        foreach (var entry in exception.Entries)
        {
            var table = Cache.Instance.TableTypeMetadata.TryGetValue(entry.Metadata.ClrType, out var metadata)
                ? metadata.TableName
                : entry.Metadata.ClrType.Name;
            var key = entry.Metadata.FindPrimaryKey();
            var id = key is { Properties.Count: 1 }
                ? AggregateDiff.AsGuid(entry.Property(key.Properties[0].Name).CurrentValue)
                : null;
            errors.Add(new AggregateError(kind, table, id, null, $"{table} {id} {reason}."));
        }

        if (errors.Count == 0)
        {
            errors.Add(new AggregateError(kind, "", null, null,
                $"The save {(kind == AggregateErrorKind.Conflict ? "conflicted with another change" : "was rejected by the database")}: " +
                $"{exception.InnerException?.Message ?? exception.Message}"));
        }

        return errors;
    }
}
