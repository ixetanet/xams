using System.Globalization;
using System.Reflection;

namespace Xams.Core.Utils;

/// <summary>
/// A field's value before and after an update.
/// </summary>
/// <param name="Old">The value read from the database.</param>
/// <param name="New">The value the save writes.</param>
public sealed record AggregateFieldChange(object? Old, object? New);

/// <summary>
/// A record <see cref="AggregateUpdater.SaveAsync"/> will insert.
/// </summary>
public sealed class AggregateCreate
{
    internal AggregateCreate(string tableName, Guid id, object entity)
    {
        TableName = tableName;
        Id = id;
        Entity = entity;
    }

    /// <summary>The table the record is inserted into.</summary>
    public string TableName { get; }

    /// <summary>The record's primary key.</summary>
    public Guid Id { get; }

    /// <summary>
    /// The instance that will be inserted, with default fields already set. Changes made to it before
    /// <see cref="AggregateUpdater.SaveAsync"/> are saved.
    /// </summary>
    public object Entity { get; }
}

/// <summary>
/// A record <see cref="AggregateUpdater.SaveAsync"/> will update. Only the fields in <see cref="Changes"/> are
/// written, plus UpdatedDate and UpdatedById when the table has them.
/// </summary>
public sealed class AggregateUpdate
{
    private readonly Dictionary<string, AggregateFieldChange> _changes;
    private readonly IReadOnlyDictionary<string, PropertyInfo> _settable;

    internal AggregateUpdate(string tableName, Guid id, Type entityType, Dictionary<string, object?> current,
        Dictionary<string, AggregateFieldChange> changes, IReadOnlyDictionary<string, PropertyInfo> settable)
    {
        TableName = tableName;
        Id = id;
        EntityType = entityType;
        CurrentValues = current;
        _changes = changes;
        _settable = settable;
    }

    /// <summary>The table the record is in.</summary>
    public string TableName { get; }

    /// <summary>The record's primary key.</summary>
    public Guid Id { get; }

    /// <summary>The fields that will be written, with their old and new values.</summary>
    public IReadOnlyDictionary<string, AggregateFieldChange> Changes => _changes;

    /// <summary>The record as read from the database before the save.</summary>
    public IReadOnlyDictionary<string, object?> Current => CurrentValues;

    internal Type EntityType { get; }

    // Read values plus any columns loaded for the update; they become the attached instance's original values
    internal Dictionary<string, object?> CurrentValues { get; }

    /// <summary>
    /// Adds or overrides a field change, for example a value computed by a business rule. Setting a field back to
    /// its current value removes the change.
    /// </summary>
    /// <param name="field">A property of the record's entity type.</param>
    /// <param name="value">The value to write. A string is converted the way the API converts field values.</param>
    /// <exception cref="ArgumentException">The field can't be written or the value doesn't fit it.</exception>
    public void Set(string field, object? value)
    {
        if (!_settable.TryGetValue(field, out var property))
        {
            throw new ArgumentException($"{TableName}.{field} can't be set on an update.", nameof(field));
        }

        var newValue = AggregateValues.Coerce(property, value);
        var oldValue = CurrentValues.GetValueOrDefault(field);
        if (_changes.TryGetValue(field, out var existing))
        {
            oldValue = existing.Old;
        }

        if (CurrentValues.ContainsKey(field) && AggregateValueComparer.AreEqual(property.PropertyType, oldValue, newValue))
        {
            _changes.Remove(field);
            return;
        }

        _changes[field] = new AggregateFieldChange(oldValue, newValue);
    }
}

/// <summary>
/// A record <see cref="AggregateUpdater.SaveAsync"/> will delete.
/// </summary>
public sealed class AggregateDelete
{
    internal AggregateDelete(string tableName, Guid id, Type entityType, Dictionary<string, object?> current)
    {
        TableName = tableName;
        Id = id;
        EntityType = entityType;
        CurrentValues = current;
    }

    /// <summary>The table the record is in.</summary>
    public string TableName { get; }

    /// <summary>The record's primary key.</summary>
    public Guid Id { get; }

    /// <summary>The record as read from the database before the save.</summary>
    public IReadOnlyDictionary<string, object?> Current => CurrentValues;

    internal Type EntityType { get; }

    internal Dictionary<string, object?> CurrentValues { get; }
}

/// <summary>
/// The IDs one table's records were created, updated or deleted with.
/// </summary>
public sealed record AggregateTableSummary(
    IReadOnlyList<Guid> Created,
    IReadOnlyList<Guid> Updated,
    IReadOnlyList<Guid> Deleted);

/// <summary>
/// Everything an <see cref="AggregateUpdater"/> save will write. Returned by <see cref="AggregateUpdater.DiffAsync"/>
/// before anything is written, so the calling action can apply business rules to it.
/// </summary>
public sealed class AggregateChangeSet
{
    internal AggregateChangeSet(List<AggregateCreate> creates, List<AggregateUpdate> updates,
        List<AggregateDelete> deletes)
    {
        Creates = creates;
        Updates = updates;
        Deletes = deletes;
    }

    /// <summary>Records that will be inserted, parents before children.</summary>
    public IReadOnlyList<AggregateCreate> Creates { get; }

    /// <summary>Records that will be updated.</summary>
    public IReadOnlyList<AggregateUpdate> Updates { get; }

    /// <summary>Records that will be deleted, children before parents.</summary>
    public IReadOnlyList<AggregateDelete> Deletes { get; }

    /// <summary>True when the save writes anything. Updates whose changes were all removed don't count.</summary>
    public bool HasChanges => Creates.Count > 0 || Deletes.Count > 0 || Updates.Any(x => x.Changes.Count > 0);

    /// <summary>The instances that will be inserted for an entity type.</summary>
    public IEnumerable<T> Created<T>() where T : class => Creates.Select(x => x.Entity).OfType<T>();

    /// <summary>The updates for a table.</summary>
    public IEnumerable<AggregateUpdate> UpdatesTo(string tableName) => Updates.Where(x => x.TableName == tableName);

    /// <summary>The deletes for a table.</summary>
    public IEnumerable<AggregateDelete> DeletesFrom(string tableName) => Deletes.Where(x => x.TableName == tableName);

    /// <summary>Created, updated and deleted IDs per table, for returning to the client.</summary>
    public IReadOnlyDictionary<string, AggregateTableSummary> Summary()
    {
        var tables = Creates.Select(x => x.TableName)
            .Concat(Updates.Where(x => x.Changes.Count > 0).Select(x => x.TableName))
            .Concat(Deletes.Select(x => x.TableName))
            .Distinct();

        return tables.ToDictionary(table => table, table => new AggregateTableSummary(
            Creates.Where(x => x.TableName == table).Select(x => x.Id).ToList(),
            Updates.Where(x => x.TableName == table && x.Changes.Count > 0).Select(x => x.Id).ToList(),
            Deletes.Where(x => x.TableName == table).Select(x => x.Id).ToList()));
    }
}

internal static class AggregateValues
{
    /// <summary>
    /// Fits a value supplied in code to a property: strings go through the API converter, other values must be
    /// assignable or convertible.
    /// </summary>
    internal static object? Coerce(PropertyInfo property, object? value)
    {
        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (value == null)
        {
            if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
            {
                throw new ArgumentException($"{property.Name} can't be null.", nameof(value));
            }

            return null;
        }

        // Before the assignable shortcut, so a string property is trimmed like a value the client sends
        if (value is string text)
        {
            try
            {
                if (EntityUtil.ConvertValue(property, text, out var converted) == ValueConversion.Skip)
                {
                    throw new ArgumentException($"{text} isn't a valid {targetType.Name} for {property.Name}.",
                        nameof(value));
                }

                return converted;
            }
            catch (Exception e) when (e is ValueConversionException or FormatException)
            {
                throw new ArgumentException(e.Message, nameof(value), e);
            }
        }

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
        {
            throw new ArgumentException($"{value} can't be written to {property.Name} ({targetType.Name}).",
                nameof(value), e);
        }
    }
}
