using System.Linq.Expressions;
using System.Reflection;

namespace Xams.Core.Utils;

/// <summary>
/// Rules for one table in an <see cref="AggregateUpdater"/> save, set with <see cref="AggregateUpdater.Table{T}"/>.
/// They apply wherever the save writes the table: as a section's root, under any join alias, and in every section.
/// </summary>
/// <typeparam name="T">The table's entity type.</typeparam>
/// <example>
/// <code>
/// .Table&lt;OrderLine&gt;(t => t
///     .Editable(x => x.Quantity, x => x.Notes)
///     .CreateOnly(x => x.ProductId)
///     .OnCreate(async (lines, ctx) =>
///     {
///         var ids = lines.Select(x => x.ProductId).Distinct().ToList();
///         var products = await db.Products.AsNoTracking()
///             .Where(x => ids.Contains(x.ProductId))
///             .ToDictionaryAsync(x => x.ProductId);
///         foreach (var line in lines)
///         {
///             if (!products.TryGetValue(line.ProductId, out var product))
///             {
///                 ctx.Reject(line, nameof(OrderLine.ProductId), "The product doesn't exist.");
///                 continue;
///             }
///
///             line.UnitPrice = product.Price;
///         }
///     }))
/// </code>
/// </example>
public sealed class AggregateTableRules<T> where T : class
{
    private readonly AggregateTableRule _rule;

    internal AggregateTableRules(AggregateTableRule rule)
    {
        _rule = rule;
    }

    /// <summary>
    /// Fields the client may write on create and on update. Once a table has Editable or CreateOnly fields, the save
    /// ignores every other field the client sends for it, including columns added to the entity later. Without
    /// either, the client may write every field the query selects.
    /// </summary>
    /// <remarks>
    /// A child join's link to its parent counts as one of its fields. Moving an existing child into another
    /// parent's array changes that link, so it's an <see cref="AggregateErrorKind.Invalid"/> error unless the link
    /// field is Editable.
    /// </remarks>
    /// <exception cref="ArgumentException">An expression isn't a property of <typeparamref name="T"/>.</exception>
    public AggregateTableRules<T> Editable(params Expression<Func<T, object?>>[] fields)
    {
        _rule.RestrictsFields = true;
        foreach (var field in FieldNames(fields))
        {
            _rule.Editable.Add(field);
        }

        return this;
    }

    /// <summary>
    /// Fields the client may set when it creates a record. After that they keep their stored value, whatever the
    /// client sends. Like <see cref="Editable"/>, this makes the save ignore every field the rules don't list.
    /// </summary>
    /// <exception cref="ArgumentException">An expression isn't a property of <typeparamref name="T"/>.</exception>
    public AggregateTableRules<T> CreateOnly(params Expression<Func<T, object?>>[] fields)
    {
        _rule.RestrictsFields = true;
        foreach (var field in FieldNames(fields))
        {
            _rule.CreateOnly.Add(field);
        }

        return this;
    }

    /// <summary>
    /// Fills in new records before anything is written. The hook gets every record of this table the save creates,
    /// from every section, in one call, so it can query what it needs once and set the fields the client can't be
    /// trusted with.
    /// </summary>
    /// <remarks>
    /// <para>Each record already has its key, its link to its parent, the query's <c>==</c> filter values (for a
    /// section's root records), the values the client may set on create, and the create defaults (owner, created and
    /// updated fields, IsActive). Every other field still has the value the entity's constructor gives it, whatever
    /// the client sent.</para>
    /// <para>The hook isn't called when the save creates none. Hooks run principal tables first: a table's hooks run
    /// after those of every table it depends on through foreign keys, directly or through other tables. Once every
    /// hook has run, the save checks that no new record's key or link to its parent changed, whichever hook changed
    /// it, then checks the records against the query's filters and writable references, as it does submitted
    /// values.</para>
    /// </remarks>
    /// <param name="fill">Sets the fields. Call <see cref="AggregateCreateContext.Reject(object, string, string)"/>
    /// to fail the save because of a record.</param>
    public AggregateTableRules<T> OnCreate(Func<IReadOnlyList<T>, AggregateCreateContext, Task> fill)
    {
        ArgumentNullException.ThrowIfNull(fill);
        _rule.OnCreate.Add((records, context) => fill(records.Cast<T>().ToList(), context));
        return this;
    }

    private static IEnumerable<string> FieldNames(Expression<Func<T, object?>>[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return fields.Select(FieldName).ToList();
    }

    // x => x.Field, with the boxing conversion a value-type property gets
    private static string FieldName(Expression<Func<T, object?>> field)
    {
        ArgumentNullException.ThrowIfNull(field);
        var body = field.Body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
            ? convert.Operand
            : field.Body;

        if (body is MemberExpression { Member: PropertyInfo property } member && member.Expression == field.Parameters[0])
        {
            return property.Name;
        }

        throw new ArgumentException(
            $"{field} doesn't name a property of {typeof(T).Name}. Use a property directly, like x => x.Name.",
            nameof(field));
    }
}

/// <summary>
/// Passed to an <see cref="AggregateTableRules{T}.OnCreate"/> hook.
/// </summary>
public sealed class AggregateCreateContext
{
    private readonly string _tableName;
    private readonly Dictionary<object, Guid> _ids;
    private readonly IReadOnlyList<object> _created;
    private readonly List<AggregateError> _errors;

    internal AggregateCreateContext(string tableName, Dictionary<object, Guid> ids, IReadOnlyList<object> created,
        List<AggregateError> errors)
    {
        _tableName = tableName;
        _ids = ids;
        _created = created;
        _errors = errors;
    }

    /// <summary>
    /// Fails the save because of one of the hook's records. The other hooks still run, then the save throws an
    /// <see cref="AggregateUpdateException"/> with every rejection as an <see cref="AggregateErrorKind.Invalid"/> error.
    /// </summary>
    /// <param name="record">One of the records passed to the hook.</param>
    /// <param name="message">Why. The error's message starts with the table and record ID, like the save's own.</param>
    public void Reject(object record, string message) => Reject(record, null, message);

    /// <summary>
    /// Fails the save because of one field of one of the hook's records. The other hooks still run, then the save
    /// throws an <see cref="AggregateUpdateException"/> with every rejection as an
    /// <see cref="AggregateErrorKind.Invalid"/> error.
    /// </summary>
    /// <param name="record">One of the records passed to the hook.</param>
    /// <param name="field">The field the error is about, or null.</param>
    /// <param name="message">Why. The error's message starts with the table and record ID, like the save's own.</param>
    public void Reject(object record, string? field, string message)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (!_ids.TryGetValue(record, out var id))
        {
            throw new ArgumentException($"Only a record passed to the {_tableName} hook can be rejected there.",
                nameof(record));
        }

        _errors.Add(new AggregateError(AggregateErrorKind.Invalid, _tableName, id, field, $"{_tableName} {id}: {message}"));
    }

    /// <summary>
    /// The records of a type the save creates, from every section. A new parent isn't in the database yet, so find
    /// it here. Hooks run principal tables first, so an ancestor table's own hooks have already filled its records.
    /// Changing a key or a link to a parent here is an error, as it is for the hook's own records.
    /// </summary>
    public IEnumerable<TRecord> Created<TRecord>() where TRecord : class => _created.OfType<TRecord>();
}

/// <summary>
/// The rules <see cref="AggregateTableRules{T}"/> collects for one table.
/// </summary>
internal sealed class AggregateTableRule(Type entityType)
{
    // The type Table<T> was called with; it must be a table's own entity type
    public Type EntityType { get; } = entityType;

    // Whether Editable or CreateOnly was called: only then are unlisted fields ignored
    public bool RestrictsFields { get; set; }
    public HashSet<string> Editable { get; } = new(StringComparer.Ordinal);
    public HashSet<string> CreateOnly { get; } = new(StringComparer.Ordinal);
    public List<Func<IReadOnlyList<object>, AggregateCreateContext, Task>> OnCreate { get; } = new();

    public bool ClientWritesOnCreate(string field) =>
        !RestrictsFields || Editable.Contains(field) || CreateOnly.Contains(field);

    public bool ClientWritesOnUpdate(string field) => !RestrictsFields || Editable.Contains(field);
}
