using Xams.Core.Entities;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// Audit records waiting to be written for one DbContext. While a DataRepository transaction is open the
/// buffer is deferred: records accumulate and merge across saves, are written just before the commit and
/// are discarded on rollback. Otherwise each save writes its own records immediately.
/// </summary>
internal sealed class AuditBuffer
{
    private readonly List<AuditRecord> _records = new();
    private readonly Dictionary<string, AuditRecord> _latest = new();
    private readonly Dictionary<string, Guid?> _creators = new();
    private readonly List<(Guid TransactionId, List<AuditRecord> Records)> _awaitingCommit = new();
    private readonly Dictionary<(string TableName, string EntityId), string> _renames = new();

    public bool Deferred { get; private set; }

    /// <summary>
    /// Identifies the audit records of a DbContext that has no DataService (and so no ExecutionId).
    /// </summary>
    public Guid ScopeId { get; private set; } = Guid.NewGuid();

    public Dictionary<Guid, AuditUser> Users { get; } = new();

    /// <summary>
    /// Lookup names read in the current scope, by lookup type and id, so later saves in the scope do not read them
    /// again. A save that writes to a lookup's table removes that table's names, because they may have changed.
    /// </summary>
    public Dictionary<(Type LookupType, object Id), string> LookupNames { get; } = new();

    /// <summary>
    /// Removes the cached names of lookups whose table the save wrote.
    /// </summary>
    public void ForgetLookupNames(IReadOnlyCollection<Type> writtenTypes)
    {
        if (writtenTypes.Count == 0 || LookupNames.Count == 0)
        {
            return;
        }

        foreach (var key in LookupNames.Keys
                     .Where(key => writtenTypes.Any(type => key.LookupType.IsAssignableFrom(type)))
                     .ToList())
        {
            LookupNames.Remove(key);
        }
    }

    /// <summary>
    /// Holds a record's new name until its audit history is renamed, just before the commit. A later rename of the
    /// same record replaces it.
    /// </summary>
    public void Rename(string tableName, string entityId, string name)
    {
        _renames[(tableName, entityId)] = name;
    }

    public bool HasRenames => _renames.Count > 0;

    public bool HasRecords => _records.Count > 0;

    /// <summary>
    /// Whether a save in the scope deleted a user, who may be the author of a record not yet written.
    /// </summary>
    public bool UserDeleted { get; private set; }

    public void NoteUserDeleted()
    {
        UserDeleted = true;
    }

    public bool IsRenamed(string tableName, string entityId)
    {
        return _renames.ContainsKey((tableName, entityId));
    }

    public List<AuditRename> TakeRenames()
    {
        var renames = _renames.Select(x => new AuditRename(x.Key.TableName, x.Key.EntityId, x.Value)).ToList();
        _renames.Clear();
        return renames;
    }

    public void BeginDeferred()
    {
        Deferred = true;
    }

    /// <summary>
    /// The most recent record for the key, which later changes by the same actor merge into.
    /// </summary>
    public AuditRecord? Latest(string key)
    {
        return _latest.GetValueOrDefault(key);
    }

    public void Add(string key, AuditRecord record)
    {
        _records.Add(record);
        _latest[key] = record;
    }

    /// <summary>
    /// Remembers who inserted an entity in the current deferred scope, so their later updates to it fold into
    /// its Create record instead of producing Update records.
    /// </summary>
    public void MarkCreated(string entityKey, Guid? actorId)
    {
        if (Deferred)
        {
            _creators[entityKey] = actorId;
        }
    }

    public bool WasCreatedBy(string entityKey, Guid? actorId)
    {
        return Deferred && _creators.TryGetValue(entityKey, out var creatorId) && creatorId == actorId;
    }

    public List<AuditRecord> TakeRecords()
    {
        var records = _records.ToList();
        _records.Clear();
        _latest.Clear();
        return records;
    }

    /// <summary>
    /// Holds records written in a transaction the caller owns until that transaction commits.
    /// </summary>
    public void AwaitCommit(Guid transactionId, List<AuditRecord> records)
    {
        if (records.Count > 0)
        {
            _awaitingCommit.Add((transactionId, records));
        }
    }

    /// <summary>
    /// Removes the records awaiting the transaction's commit. Records of any other transaction are dropped:
    /// transactions on a DbContext run one at a time, so those ended without a commit.
    /// </summary>
    public List<AuditRecord> TakeAwaitingCommit(Guid transactionId)
    {
        var records = _awaitingCommit
            .Where(x => x.TransactionId == transactionId)
            .SelectMany(x => x.Records)
            .ToList();
        _awaitingCommit.Clear();
        return records;
    }

    /// <summary>
    /// Discards pending records and ends the deferred scope.
    /// </summary>
    public void Reset()
    {
        _records.Clear();
        _latest.Clear();
        _creators.Clear();
        _renames.Clear();
        LookupNames.Clear();
        UserDeleted = false;
        Deferred = false;
        ScopeId = Guid.NewGuid();
    }
}

internal sealed class AuditRecord
{
    public required AuditHistory History { get; init; }

    /// <summary>
    /// The executing user id that produced the record (before any unknown-user fallback).
    /// </summary>
    public Guid? ActorId { get; init; }

    public Dictionary<string, AuditHistoryDetail> Details { get; } = new();

    /// <summary>
    /// The untruncated old and new values of each update detail, used to recognize a field that changed back.
    /// </summary>
    public Dictionary<string, (object? Old, object? New)> Values { get; } = new();
}

internal sealed record AuditUser(Guid? UserId, string? UserName);

internal sealed record AuditRename(string TableName, string EntityId, string Name);
