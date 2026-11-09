using System.Text.Json;
using Xams.Core.Base;

namespace Xams.Core.Utils;

/// <summary>
/// Saves data that was read with <see cref="ServiceQuery"/>s back to the database: works out which root and joined
/// records to create, update and delete, then writes only what changed with one SaveChanges.
/// </summary>
/// <remarks>
/// <para>The save does not run the Xams pipeline: no ServiceLogic or bulk hooks, no UI validation, no permission
/// checks and no Xams cascade traversal. Audit follows each table's settings; call SetAuditEnabled(false) on the
/// XamsDbContext before saving to skip it. The writes go through the context's DbContext, so inside an action they
/// commit or roll back with the action.</para>
/// <para>Give each section the same query the data was read with. Rows the query selects but the submitted data
/// leaves out are deleted; a missing or null section or join array leaves its rows alone, while an empty array
/// deletes them.</para>
/// <para>For a table whose values the client can't be trusted with, <see cref="Table{T}"/> limits the fields the
/// client may write and fills the rest in on create.</para>
/// </remarks>
/// <example>
/// <code>
/// var changes = await new AggregateUpdater(context)
///     .Record(TaskQueries.Task(input.TaskId), input.Task)
///     .Collection(TaskQueries.Travel(input.TaskId), input.Travels)
///     .LoadedAt(input.LoadedAt)
///     .SaveAsync();
/// </code>
/// </example>
public sealed class AggregateUpdater
{
    private readonly BaseServiceContext _context;
    private readonly List<AggregateSection> _sections = new();
    private readonly Dictionary<Type, AggregateTableRule> _tableRules = new();
    private DateTime? _loadedAt;
    private bool _ignoreConflicts;
    private AggregateChangeSet? _changes;
    private DateTime _now;
    private bool _saved;

    /// <param name="context">The action, service or job context whose DbContext the save writes through.</param>
    public AggregateUpdater(BaseServiceContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Adds a section whose query selects many root records.
    /// </summary>
    /// <param name="query">The query the data was read with.</param>
    /// <param name="rows">The edited records as a JSON array. Null leaves the section's rows alone.</param>
    /// <param name="options">Per-section options, such as making a reference join writable.</param>
    public AggregateUpdater Collection(ServiceQuery query, JsonElement? rows,
        Action<AggregateSectionOptions>? options = null) => AddSection(query, rows, options, false);

    /// <summary>
    /// Adds a section whose query selects one root record. The root record is never deleted.
    /// </summary>
    /// <param name="query">The query the data was read with.</param>
    /// <param name="record">The edited record as a JSON object. Null leaves the section's rows alone.</param>
    /// <param name="options">Per-section options, such as making a reference join writable.</param>
    public AggregateUpdater Record(ServiceQuery query, JsonElement? record,
        Action<AggregateSectionOptions>? options = null) => AddSection(query, record, options, true);

    /// <summary>
    /// Sets rules for one table wherever the save writes it: as a section's root, under any join alias, and in every
    /// section. Use it for a table whose values the client can't be trusted with: list the fields the client may
    /// write, and fill the others in with an <see cref="AggregateTableRules{T}.OnCreate"/> hook. Calling it again
    /// for the same table adds to its rules.
    /// </summary>
    /// <typeparam name="T">The table's entity type, as its DbSet declares it. A base class isn't accepted.</typeparam>
    /// <example>
    /// <code>
    /// .Table&lt;OrderLine&gt;(t => t
    ///     .Editable(x => x.Quantity, x => x.Notes)   // the only fields the client may change
    ///     .CreateOnly(x => x.ProductId)              // set by the client on create, kept after that
    ///     .OnCreate(async (lines, ctx) => { /* query the prices and set line.UnitPrice */ }))
    /// </code>
    /// </example>
    public AggregateUpdater Table<T>(Action<AggregateTableRules<T>> rules) where T : class
    {
        ArgumentNullException.ThrowIfNull(rules);
        EnsureNotDiffed();
        if (!_tableRules.TryGetValue(typeof(T), out var rule))
        {
            rule = new AggregateTableRule(typeof(T));
            _tableRules[typeof(T)] = rule;
        }

        rules(new AggregateTableRules<T>(rule));
        return this;
    }

    /// <summary>
    /// When the data was loaded. Deleting a row that someone else created or changed after this time is a
    /// conflict instead.
    /// </summary>
    public AggregateUpdater LoadedAt(DateTime loadedAtUtc)
    {
        EnsureNotDiffed();
        _loadedAt = loadedAtUtc;
        return this;
    }

    /// <summary>
    /// Skips the conflict checks: a stale UpdatedDate on a changed record and the <see cref="LoadedAt"/> guard.
    /// </summary>
    public AggregateUpdater IgnoreConflicts()
    {
        EnsureNotDiffed();
        _ignoreConflicts = true;
        return this;
    }

    /// <summary>
    /// Reads the current rows and works out the changes without writing anything. The calling action can inspect
    /// the result and apply business rules to it before <see cref="SaveAsync"/>.
    /// </summary>
    /// <exception cref="AggregateUpdateException">The query or the submitted data can't be saved.</exception>
    public async Task<AggregateChangeSet> DiffAsync()
    {
        if (_changes != null)
        {
            return _changes;
        }

        var db = _context.GetDbContext<IXamsDbContext>();
        var errors = new List<AggregateError>();
        var rules = AggregateTree.ResolveRules(_tableRules.Values, db.Model, errors);
        foreach (var section in _sections)
        {
            AggregateTree.Build(section, db.Model, rules, errors);
        }

        if (errors.Count > 0)
        {
            throw new AggregateUpdateException(errors);
        }

        _now = DateTime.UtcNow;
        _changes = await new AggregateDiff(_context, _now, _loadedAt, _ignoreConflicts, rules).RunAsync(_sections);
        return _changes;
    }

    /// <summary>
    /// Writes the changes with one SaveChanges, using the <see cref="DiffAsync"/> result if it already ran. Any
    /// writes the caller staged on the same DbContext are saved with them.
    /// </summary>
    /// <exception cref="AggregateUpdateException">The diff found problems or the database rejected the save.</exception>
    public async Task<AggregateChangeSet> SaveAsync()
    {
        if (_saved)
        {
            throw new InvalidOperationException("This AggregateUpdater has already saved.");
        }

        var changes = await DiffAsync();
        _saved = true;
        await AggregateWriter.SaveAsync(_context.GetDbContext<IXamsDbContext>(), changes, _context.ExecutingUserId,
            _now);
        return changes;
    }

    private AggregateUpdater AddSection(ServiceQuery query, JsonElement? submitted,
        Action<AggregateSectionOptions>? configure, bool isRecord)
    {
        ArgumentNullException.ThrowIfNull(query);
        EnsureNotDiffed();
        var options = new AggregateSectionOptions();
        configure?.Invoke(options);
        _sections.Add(new AggregateSection
        {
            Query = query,
            Submitted = submitted,
            Options = options,
            IsRecord = isRecord
        });
        return this;
    }

    private void EnsureNotDiffed()
    {
        if (_changes != null)
        {
            throw new InvalidOperationException("The AggregateUpdater can't be changed after DiffAsync or SaveAsync.");
        }
    }
}

/// <summary>
/// Options for one <see cref="AggregateUpdater"/> section.
/// </summary>
public sealed class AggregateSectionOptions
{
    internal HashSet<string> WritableAliases { get; } = new(StringComparer.Ordinal);
    internal HashSet<string> ReadOnlyAliases { get; } = new(StringComparer.Ordinal);
    internal HashSet<string> ReadOnlyFieldNames { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Makes a reference (many-to-one) join writable: its records can be created and updated, never deleted.
    /// Editing a reference record changes it for every record that points at it.
    /// </summary>
    public AggregateSectionOptions Writable(string alias)
    {
        WritableAliases.Add(alias);
        return this;
    }

    /// <summary>
    /// Makes a child (one-to-many) join read-only: its records, and any joins under it, are not saved.
    /// </summary>
    public AggregateSectionOptions ReadOnly(string alias)
    {
        ReadOnlyAliases.Add(alias);
        return this;
    }

    /// <summary>
    /// Fields the save never writes, as "Field" for the root table or "alias.Field" for a join. To protect a
    /// table's fields wherever it appears, list the fields the client may write with
    /// <see cref="AggregateUpdater.Table{T}"/> instead.
    /// </summary>
    public AggregateSectionOptions ReadOnlyFields(params string[] fields)
    {
        foreach (var field in fields)
        {
            ReadOnlyFieldNames.Add(field);
        }

        return this;
    }
}

/// <summary>
/// Shorthand for saving one query's data.
/// </summary>
public static class ServiceQueryAggregateExtensions
{
    /// <summary>
    /// Creates an <see cref="AggregateUpdater"/> with this query as its only section.
    /// </summary>
    /// <example><c>await TaskQueries.Travel(taskId).ToUpdater(context, input.Travels).SaveAsync();</c></example>
    public static AggregateUpdater ToUpdater(this ServiceQuery query, BaseServiceContext context, JsonElement? rows,
        Action<AggregateSectionOptions>? options = null) =>
        new AggregateUpdater(context).Collection(query, rows, options);
}
