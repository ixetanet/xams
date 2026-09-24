using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Entities;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// Records create, update and delete audit history for changes saved through an XamsDbContext.
/// </summary>
/// <remarks>
/// Old values are read on the saving connection before the write, so they are the values inside the current
/// transaction. Inside a DataRepository transaction the records are buffered on the DbContext and written just
/// before the commit; any other save writes its records in the same transaction as the change. Records are
/// inserted by <see cref="AuditRowWriter"/>, not through SaveChanges, so tracked entries are never touched.
/// The synchronous SaveChanges runs the same code with EF's synchronous calls (<c>async</c> false), so every
/// awaited task has already completed and nothing is waited on.
/// </remarks>
public static class AuditLogic
{
    internal const int NameMaxLength = 250;
    internal const int EntityIdMaxLength = 250;
    internal const int UserNameMaxLength = 250;
    internal const int FieldTypeMaxLength = 30;
    internal const int ValueMaxLength = 8000;
    private const int QueryBatchSize = 500;

    // Distinct from the savepoint SaveChanges creates itself
    private const string SavepointName = "__XamsAuditSavePoint";

    private static readonly HashSet<Type> NeverAudited =
        [typeof(AuditHistory), typeof(AuditHistoryDetail), typeof(Entities.System)];

    private static readonly ConcurrentDictionary<IEntityType, EntityShape> Shapes = new();
    private static readonly ConcurrentDictionary<Type, byte> MissingInterceptorWarnings = new();

    private static readonly MethodInfo FindTypedMethod = typeof(AuditLogic)
        .GetMethod(nameof(FindTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<(Type Entity, Type Key), FindRows> Finders = new();

    private delegate Task<List<object>> FindRows(IXamsDbContext db, PropertyInfo keyProperty, PropertyInfo[]? columns,
        object[] ids, bool async, CancellationToken cancellationToken);

    internal static Task<int> SaveChangesAsync(IXamsDbContext db, bool acceptAllChangesOnSuccess,
        Func<bool, Task<int>> save, CancellationToken cancellationToken)
    {
        return SaveAsync(db, acceptAllChangesOnSuccess, save, async: true, cancellationToken);
    }

    internal static int SaveChanges(IXamsDbContext db, bool acceptAllChangesOnSuccess, Func<bool, int> save)
    {
        // Synchronous throughout, so the task has completed by the time it returns
        return SaveAsync(db, acceptAllChangesOnSuccess, accept => Task.FromResult(save(accept)), async: false,
            CancellationToken.None).GetAwaiter().GetResult();
    }

    private static async Task<int> SaveAsync(IXamsDbContext db, bool acceptAllChangesOnSuccess,
        Func<bool, Task<int>> save, bool async, CancellationToken cancellationToken)
    {
        var pending = CollectPending(db);
        if (pending.Operations.Count == 0)
        {
            return await pending.SaveAsync(db.GetAuditBuffer(), () => save(acceptAllChangesOnSuccess));
        }

        return await SaveWithAuditAsync(db, pending, acceptAllChangesOnSuccess, save, async, cancellationToken);
    }

    private static async Task<int> SaveWithAuditAsync(IXamsDbContext db, PendingSave pending,
        bool acceptAllChangesOnSuccess, Func<bool, Task<int>> save, bool async, CancellationToken cancellationToken)
    {
        var buffer = db.GetAuditBuffer();
        if (buffer.Deferred)
        {
            // The DataRepository writes these records before its commit and discards them on rollback
            return await AuditedSaveAsync(db, pending, () => save(acceptAllChangesOnSuccess), async,
                cancellationToken);
        }

        var callerTransaction = db.Database.CurrentTransaction;
        var ambientTransaction = Transaction.Current;
        if (callerTransaction != null || ambientTransaction != null)
        {
            // The caller's transaction covers the change and its audit records; OnCreateAudit waits for its commit
            int result;
            List<AuditRecord> written;
            try
            {
                (result, written) = await SaveInCallerTransactionAsync(db, callerTransaction, pending,
                    () => save(false), async, cancellationToken);
            }
            finally
            {
                buffer.Reset();
            }

            // Changes are accepted only once their audit records are written too
            if (acceptAllChangesOnSuccess)
            {
                db.ChangeTracker.AcceptAllChanges();
            }

            if (callerTransaction != null)
            {
                PublishOnCommit(db, callerTransaction.TransactionId, written);
            }
            else
            {
                PublishOnCommit(db, ambientTransaction!, written);
            }

            return result;
        }

        // Otherwise the change and its audit records are committed together in a transaction opened here
        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            if (!strategy.RetriesOnFailure)
            {
                var (result, written) = await SaveInTransactionAsync(db, pending,
                    () => save(acceptAllChangesOnSuccess), async, cancellationToken);
                await PublishAsync(db, written, async);
                return result;
            }

            // A retrying strategy reruns the whole unit, so the changes are only accepted once it has committed
            var retried = async
                ? await strategy.ExecuteAsync(retryCancellationToken =>
                {
                    buffer.Reset();
                    return SaveInTransactionAsync(db, pending, () => save(false), true, retryCancellationToken);
                }, cancellationToken)
                : strategy.Execute(() =>
                {
                    buffer.Reset();
                    return SaveInTransactionAsync(db, pending, () => save(false), false, CancellationToken.None)
                        .GetAwaiter().GetResult();
                });
            if (acceptAllChangesOnSuccess)
            {
                db.ChangeTracker.AcceptAllChanges();
            }

            await PublishAsync(db, retried.Written, async);
            return retried.Result;
        }
        finally
        {
            buffer.Reset();
        }
    }

    private static async Task<(int Result, List<AuditRecord> Written)> SaveInTransactionAsync(IXamsDbContext db,
        PendingSave pending, Func<Task<int>> save, bool async, CancellationToken cancellationToken)
    {
        var transaction = async
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : db.Database.BeginTransaction();
        try
        {
            var result = await AuditedSaveAsync(db, pending, save, async, cancellationToken);
            var written = await WriteAsync(db, db.GetAuditBuffer().TakeRecords(), async, cancellationToken);
            if (async)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                transaction.Commit();
            }

            return (result, written);
        }
        finally
        {
            if (async)
            {
                await transaction.DisposeAsync();
            }
            else
            {
                transaction.Dispose();
            }
        }
    }

    /// <summary>
    /// Saves the change and writes its audit records in a transaction the caller opened. As SaveChanges does
    /// there, a failure rolls back to a savepoint, so neither the change nor any of its audit records remain and
    /// the transaction can still be used. The savepoint takes the place of the one SaveChanges would create.
    /// </summary>
    private static async Task<(int Result, List<AuditRecord> Written)> SaveInCallerTransactionAsync(
        IXamsDbContext db, IDbContextTransaction? transaction, PendingSave pending, Func<Task<int>> save,
        bool async, CancellationToken cancellationToken)
    {
        var autoSavepoints = db.Database.AutoSavepointsEnabled;
        if (transaction is not { SupportsSavepoints: true } || !autoSavepoints)
        {
            var result = await AuditedSaveAsync(db, pending, save, async, cancellationToken);
            return (result, await WriteAsync(db, db.GetAuditBuffer().TakeRecords(), async, cancellationToken));
        }

        if (async)
        {
            await transaction.CreateSavepointAsync(SavepointName, cancellationToken);
        }
        else
        {
            transaction.CreateSavepoint(SavepointName);
        }

        db.Database.AutoSavepointsEnabled = false;
        try
        {
            var result = await AuditedSaveAsync(db, pending, save, async, cancellationToken);
            var written = await WriteAsync(db, db.GetAuditBuffer().TakeRecords(), async, cancellationToken);
            if (async)
            {
                await transaction.ReleaseSavepointAsync(SavepointName, cancellationToken);
            }
            else
            {
                transaction.ReleaseSavepoint(SavepointName);
            }

            return (result, written);
        }
        catch
        {
            try
            {
                if (async)
                {
                    await transaction.RollbackToSavepointAsync(SavepointName, CancellationToken.None);
                }
                else
                {
                    transaction.RollbackToSavepoint(SavepointName);
                }
            }
            catch (Exception e)
            {
                // Report the save's own failure; the database may already have ended the transaction
                Log(db, e, $"Rolling back a failed audited save to its savepoint failed: {e.Message}");
            }

            throw;
        }
        finally
        {
            db.Database.AutoSavepointsEnabled = autoSavepoints;
        }
    }

    /// <summary>
    /// Saves the change and adds its audit records to the buffer. Old values and old lookup names are read before
    /// the write, on the saving connection, so they are the values as this transaction sees them.
    /// </summary>
    private static async Task<int> AuditedSaveAsync(IXamsDbContext db, PendingSave pending, Func<Task<int>> save,
        bool async, CancellationToken cancellationToken)
    {
        var buffer = db.GetAuditBuffer();
        var operations = pending.Operations;
        var lookupNames = new Dictionary<(Type, object), string?>();
        await CapturePreImagesAsync(db, operations, async, cancellationToken);
        // The save cannot change the names of lookup tables it does not write, so their new names are read with
        // the old names; the others are read after the save
        await ResolveLookupNamesAsync(db, buffer, OldLookupValues(operations)
                .Concat(NewLookupValues(operations).Where(x => !pending.Writes(x.Field.LookupType!))),
            lookupNames, async, cancellationToken);

        var result = await pending.SaveAsync(buffer, save);

        AddGeneratedChanges(operations);
        await ResolveLookupNamesAsync(db, buffer, OldLookupValues(operations).Concat(NewLookupValues(operations)),
            lookupNames, async, cancellationToken);
        await AddRecordsAsync(db, buffer, operations, lookupNames, async, cancellationToken);
        return result;
    }

    private static void PublishOnCommit(IXamsDbContext db, Guid transactionId, List<AuditRecord> written)
    {
        if (written.Count == 0 || db.OnCreateAudit == null)
        {
            return;
        }

        if (!db.PublishesAuditOnCommit)
        {
            // Without the transaction interceptor the outcome cannot be observed, and publishing now could
            // report changes that are later rolled back
            if (MissingInterceptorWarnings.TryAdd(db.GetType(), 0))
            {
                Log(db, null, $"OnCreateAudit is skipped for transactions opened on {db.GetType().Name}: " +
                              "its OnConfiguring must call base.OnConfiguring to register the audit interceptor.");
            }

            return;
        }

        db.GetAuditBuffer().AwaitCommit(transactionId, written);
    }

    private static void PublishOnCommit(IXamsDbContext db, Transaction ambientTransaction, List<AuditRecord> written)
    {
        if (written.Count == 0 || db.OnCreateAudit == null)
        {
            return;
        }

        ambientTransaction.TransactionCompleted += (_, e) =>
        {
            if (e.Transaction?.TransactionInformation.Status == TransactionStatus.Committed)
            {
                // The scope completes synchronously, and OnCreateAudit is asynchronous
                PublishAsync(db, written, async: false).GetAwaiter().GetResult();
            }
        };
    }

    /// <summary>
    /// Inserts the audit records in the DbContext's current transaction.
    /// </summary>
    internal static async Task<List<AuditRecord>> WriteAsync(IXamsDbContext db, List<AuditRecord> records,
        bool async = true, CancellationToken cancellationToken = default)
    {
        // An update whose fields all changed back to their original values leaves nothing to record
        records = records
            .Where(x => x.History.Operation != nameof(DataOperation.Update) || x.Details.Count > 0)
            .ToList();
        if (records.Count == 0)
        {
            return records;
        }

        // A user deleted after making a buffered change can no longer be referenced; the captured name remains.
        // Only a deletion this scope saved can do that, so the check is skipped otherwise.
        var userIds = records.Select(x => x.History.UserId).OfType<Guid>().Distinct().ToList();
        if (userIds.Count > 0 && db.GetAuditBuffer().UserDeleted)
        {
            var query = db.UsersBase.AsNoTracking()
                .Where(x => userIds.Contains(x.UserId))
                .Select(x => x.UserId);
            var existingUserIds = (async ? await query.ToListAsync(cancellationToken) : query.ToList()).ToHashSet();
            foreach (var record in records.Where(x => x.History.UserId is { } userId &&
                                                      !existingUserIds.Contains(userId)))
            {
                record.History.UserId = null;
            }
        }

        try
        {
            await AuditRowWriter.WriteAsync(db, records.Select(x => x.History).ToList(),
                records.SelectMany(x => x.Details.Values).ToList(), async, cancellationToken);
        }
        catch (Exception e) when (e is not (DbUpdateException or OperationCanceledException))
        {
            // As SaveChanges reports a failed write, which is also what execution strategies unwrap
            throw new DbUpdateException("An error occurred while saving the audit records. " +
                                        "See the inner exception for details.", e);
        }

        return records;
    }

    /// <summary>
    /// Invokes OnCreateAudit with committed records. A failing handler is logged, not rethrown, because the
    /// audited changes can no longer be rolled back.
    /// </summary>
    /// <remarks>
    /// OnCreateAudit is asynchronous, so a synchronous caller waits for it on the thread pool, where a
    /// synchronization context cannot deadlock it.
    /// </remarks>
    internal static async Task PublishAsync(IXamsDbContext db, List<AuditRecord> records, bool async = true)
    {
        if (records.Count == 0 || db.OnCreateAudit == null)
        {
            return;
        }

        if (!async)
        {
            Task.Run(() => PublishAsync(db, records)).GetAwaiter().GetResult();
            return;
        }

        var auditContext = new AuditContext
        {
            XamsDbContext = db,
            DataService = db.GetDataService(),
            AuditHistoryEntities = records
                .GroupBy(x => x.History.TableName ?? "")
                .ToDictionary(x => x.Key, x => x.Select(record => new AuditHistoryEntity
                {
                    AuditHistory = record.History,
                    AuditHistoryDetails = record.Details
                }).ToList())
        };

        try
        {
            await db.OnCreateAudit(auditContext);
        }
        catch (Exception e)
        {
            Log(db, e, $"OnCreateAudit failed: {e.Message}");
        }
    }

    internal static void Log(IXamsDbContext db, Exception? exception, string message)
    {
        var logger = db.GetDataService()?.GetLogger();
        if (logger == null)
        {
            Console.WriteLine(exception != null ? $"{message}{Environment.NewLine}{exception}" : message);
        }
        else if (exception != null)
        {
            logger.LogError(exception, "{Message}", message);
        }
        else
        {
            logger.LogWarning("{Message}", message);
        }
    }

    public static string? FormatValue(object? value)
    {
        return value switch
        {
            null => null,
            // Stored dates are UTC; providers other than Postgres read them back without a kind
            DateTime { Kind: DateTimeKind.Unspecified } dateTime => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                .ToString("O", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToBase64String(bytes),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// What the save writes that auditing needs to know about. The change tracker is enumerated (and its changes
    /// detected) at most once, and not at all when nothing is audited and the scope holds nothing.
    /// </summary>
    private static PendingSave CollectPending(IXamsDbContext db)
    {
        var pending = new PendingSave();
        var configuration = Cache.Instance.AuditConfigurationSnapshot;
        var buffer = db.GetAuditBuffer();
        var auditing = db.GetAuditEnabled() && configuration.AnyEnabled;
        // Cached lookup names, queued renames and buffered records depend on what later saves in the scope write
        var following = auditing || buffer.LookupNames.Count > 0 || buffer.HasRenames || buffer.HasRecords;
        var notePendingChanges = !db.SaveChangesCalledWithPendingChanges();
        if (!following && !notePendingChanges)
        {
            return pending;
        }

        var actorId = ActorId(db);
        foreach (var entry in db.ChangeTracker.Entries())
        {
            DataOperation? operation = entry.State switch
            {
                EntityState.Added => DataOperation.Create,
                EntityState.Modified => DataOperation.Update,
                EntityState.Deleted => DataOperation.Delete,
                _ => null
            };
            if (operation == null)
            {
                continue;
            }

            if (notePendingChanges)
            {
                db.NotePendingChanges();
                notePendingChanges = false;
            }

            if (!following)
            {
                break;
            }

            pending.WrittenTypes.Add(entry.Metadata.ClrType);
            if (operation is DataOperation.Delete)
            {
                if (entry.Entity is User)
                {
                    buffer.NoteUserDeleted();
                }
            }
            else if (buffer.HasRenames)
            {
                NoteNameWrite(buffer, entry, pending.NameWrites);
            }

            if (!auditing)
            {
                continue;
            }

            var entityType = entry.Entity.GetType();
            if (NeverAudited.Contains(entityType) ||
                !Cache.Instance.TableTypeMetadata.TryGetValue(entityType, out var metadata) ||
                !configuration.Tables.TryGetValue(metadata.TableName, out var auditInfo))
            {
                continue;
            }

            // In a deferred scope creates are tracked and updates kept even when only the other flag is on,
            // because an update to a row created in the same transaction folds into its Create record
            var relevant = operation switch
            {
                DataOperation.Create => auditInfo.IsCreateAuditEnabled ||
                                        (buffer.Deferred && auditInfo.IsUpdateAuditEnabled),
                DataOperation.Update => auditInfo.IsUpdateAuditEnabled ||
                                        (buffer.Deferred && auditInfo.IsCreateAuditEnabled),
                _ => auditInfo.IsDeleteAuditEnabled
            };
            if (!relevant)
            {
                continue;
            }

            var shape = GetShape(db, entityType, metadata);
            if (shape == null)
            {
                continue;
            }

            var pendingOperation = new PendingOperation
            {
                Entity = entry.Entity,
                Shape = shape,
                AuditInfo = auditInfo,
                Operation = operation.Value
            };
            if (operation is DataOperation.Update)
            {
                // Only properties EF writes are compared; unmodified values on an attached instance may be defaults
                pendingOperation.ModifiedProperties = entry.Properties
                    .Where(x => x.IsModified)
                    .Select(x => x.Metadata.Name)
                    .ToHashSet();
                pendingOperation.FoldIntoCreate =
                    buffer.WasCreatedBy(EntityKey(shape, shape.GetId(entry.Entity)), actorId);
                // An update that writes no audited field has nothing to record, so its old values are not read
                if (!pendingOperation.WritesAuditedField())
                {
                    continue;
                }
            }

            pending.Operations.Add(pendingOperation);
        }

        return pending;
    }

    /// <summary>
    /// Notes the new name the save writes for an entity whose audit history is waiting to be renamed, so the
    /// history is not renamed to an earlier name.
    /// </summary>
    private static void NoteNameWrite(AuditBuffer buffer, EntityEntry entry, List<AuditRename> nameWrites)
    {
        if (!Cache.Instance.TableTypeMetadata.TryGetValue(entry.Entity.GetType(), out var metadata) ||
            metadata.NameProperty is not { } nameProperty ||
            metadata.PrimaryKeyProperty.GetValue(entry.Entity) is not { } id)
        {
            return;
        }

        var entityId = FormatId(id);
        if (!buffer.IsRenamed(metadata.TableName, entityId) ||
            (entry.State is EntityState.Modified &&
             entry.Properties.FirstOrDefault(x => x.Metadata.Name == nameProperty.Name) is not { IsModified: true }))
        {
            return;
        }

        nameWrites.Add(new AuditRename(metadata.TableName, entityId, RecordName(entry.Entity)));
    }

    private static async Task CapturePreImagesAsync(IXamsDbContext db, List<PendingOperation> operations,
        bool async, CancellationToken cancellationToken)
    {
        var needPreImages = operations
            .Where(x => x.Operation is DataOperation.Delete ||
                        (x.Operation is DataOperation.Update && !x.FoldIntoCreate &&
                         x.AuditInfo.IsUpdateAuditEnabled))
            .GroupBy(x => x.Entity.GetType());

        foreach (var group in needPreImages)
        {
            var shape = group.First().Shape;
            var columns = shape.StoredColumns(group.First().AuditInfo, group.Select(x => x.Operation).Distinct());
            var rows = await ReadStoredRowsAsync(db, group.Key, shape, columns,
                group.Select(x => shape.GetId(x.Entity)).Distinct().ToList(), async, cancellationToken);

            foreach (var operation in group)
            {
                operation.PreImage = rows.GetValueOrDefault(shape.GetId(operation.Entity));
                if (operation.Operation is DataOperation.Update && operation.PreImage != null)
                {
                    operation.ChangedFields = operation.Shape.Fields
                        .Where(x => !x.GeneratedOnUpdate && operation.ModifiedProperties.Contains(x.Name))
                        .Where(x => IsFieldEnabled(operation.AuditInfo, x, DataOperation.Update))
                        .Where(x => !ValuesEqual(operation.PreImage[x], x.Property.GetValue(operation.Entity)))
                        .ToList();
                }
            }
        }
    }

    /// <summary>
    /// Reads the stored values of rows by key: the given columns, or whole entities when columns is null.
    /// </summary>
    private static async Task<Dictionary<object, StoredRow>> ReadStoredRowsAsync(IXamsDbContext db,
        Type entityType, EntityShape shape, IReadOnlyList<FieldShape>? columns, List<object> ids, bool async,
        CancellationToken cancellationToken)
    {
        var rows = await FindAsync(db, entityType, shape.KeyProperty, columns?.Select(x => x.Property).ToArray(),
            ids, async, cancellationToken);
        return rows.ToDictionary(x => x.Key, x => columns == null
            ? StoredRow.FromEntity(x.Value)
            : StoredRow.FromColumns(columns, (object?[])x.Value, shape.NameProperty));
    }

    /// <summary>
    /// Adds changes to values the database generates on update, which EF reads back into the entity by the save.
    /// </summary>
    private static void AddGeneratedChanges(List<PendingOperation> operations)
    {
        foreach (var operation in operations.Where(x => x.Operation is DataOperation.Update && x.PreImage != null))
        {
            operation.ChangedFields.AddRange(operation.Shape.Fields
                .Where(x => x.GeneratedOnUpdate)
                .Where(x => IsFieldEnabled(operation.AuditInfo, x, DataOperation.Update))
                .Where(x => !ValuesEqual(operation.PreImage![x], x.Property.GetValue(operation.Entity))));
        }
    }

    private static IEnumerable<(FieldShape Field, object? Value)> OldLookupValues(List<PendingOperation> operations)
    {
        foreach (var operation in operations.Where(x => x.PreImage != null))
        {
            var fields = operation.Operation is DataOperation.Delete
                ? operation.Shape.Fields.Where(x => IsFieldEnabled(operation.AuditInfo, x, DataOperation.Delete))
                : operation.ChangedFields;
            foreach (var field in fields.Where(x => x.LookupType != null))
            {
                yield return (field, operation.PreImage![field]);
            }
        }
    }

    private static IEnumerable<(FieldShape Field, object? Value)> NewLookupValues(List<PendingOperation> operations)
    {
        foreach (var operation in operations)
        {
            IEnumerable<FieldShape> fields = operation switch
            {
                { Operation: DataOperation.Create, AuditInfo.IsCreateAuditEnabled: true } =>
                    operation.Shape.Fields.Where(x => IsFieldEnabled(operation.AuditInfo, x, DataOperation.Create)),
                { FoldIntoCreate: true, AuditInfo.IsCreateAuditEnabled: true } => FoldedFields(operation),
                _ => operation.ChangedFields
            };
            foreach (var field in fields.Where(x => x.LookupType != null))
            {
                yield return (field, field.Property.GetValue(operation.Entity));
            }
        }
    }

    /// <summary>
    /// Resolves lookup names with one query per lookup table, reusing names already read in the scope. Only the
    /// key and name columns are read. A missing target is recorded without a name rather than failing a change
    /// that has already been written.
    /// </summary>
    private static async Task ResolveLookupNamesAsync(IXamsDbContext db, AuditBuffer buffer,
        IEnumerable<(FieldShape Field, object? Value)> lookupValues, Dictionary<(Type, object), string?> names,
        bool async, CancellationToken cancellationToken)
    {
        var missing = new Dictionary<Type, (FieldShape Field, HashSet<object> Ids)>();
        foreach (var (field, value) in lookupValues)
        {
            if (value == null || names.ContainsKey((field.LookupType!, value)))
            {
                continue;
            }

            if (buffer.LookupNames.TryGetValue((field.LookupType!, value), out var cached))
            {
                names[(field.LookupType!, value)] = cached;
                continue;
            }

            if (!missing.TryGetValue(field.LookupType!, out var group))
            {
                group = (field, []);
                missing[field.LookupType!] = group;
            }

            group.Ids.Add(value);
        }

        foreach (var (lookupType, (field, ids)) in missing)
        {
            var rows = await FindAsync(db, lookupType, field.LookupKeyProperty!, field.LookupNameColumns,
                ids.ToList(), async, cancellationToken);
            foreach (var id in ids)
            {
                var name = !rows.TryGetValue(id, out var row) ? null
                    : field.LookupNameColumns == null ? row.GetNameFieldValue()
                    : field.LookupNameColumns.Length == 0 ? string.Empty
                    : ((object?[])row)[1] as string;
                names[(lookupType, id)] = name;
                if (name != null)
                {
                    buffer.LookupNames[(lookupType, id)] = name;
                }
            }
        }
    }

    private static async Task AddRecordsAsync(IXamsDbContext db, AuditBuffer buffer,
        List<PendingOperation> operations, Dictionary<(Type, object), string?> lookupNames, bool async,
        CancellationToken cancellationToken)
    {
        var dataService = db.GetDataService();
        var actorId = ActorId(db);
        var transactionId = dataService?.GetExecutionId() ?? buffer.ScopeId;
        var newId = AuditIds.For(db);
        AuditUser? user = null;

        async Task<AuditRecord> NewRecord(PendingOperation operation, DataOperation dataOperation, object id,
            string name)
        {
            user ??= actorId != null
                ? await GetUserAsync(db, buffer, actorId.Value, async, cancellationToken)
                : new AuditUser(null, null);

            return new AuditRecord
            {
                ActorId = actorId,
                History = new AuditHistory
                {
                    AuditHistoryId = newId(),
                    Name = name,
                    TableName = operation.Shape.TableName,
                    EntityId = FormatId(id).Truncate(EntityIdMaxLength),
                    Operation = dataOperation.ToString(),
                    UserId = user.UserId,
                    UserName = user.UserName.Truncate(UserNameMaxLength),
                    CreatedDate = DateTime.UtcNow,
                    TransactionId = transactionId
                }
            };
        }

        foreach (var operation in operations)
        {
            var id = operation.Shape.GetId(operation.Entity);
            var entityKey = EntityKey(operation.Shape, id);
            var createKey = RecordKey(DataOperation.Create, entityKey);

            switch (operation.Operation)
            {
                case DataOperation.Create:
                {
                    buffer.MarkCreated(entityKey, actorId);
                    if (!operation.AuditInfo.IsCreateAuditEnabled)
                    {
                        break;
                    }

                    var record = await NewRecord(operation, DataOperation.Create, id, RecordName(operation.Entity));
                    foreach (var field in operation.Shape.Fields
                                 .Where(x => IsFieldEnabled(operation.AuditInfo, x, DataOperation.Create)))
                    {
                        var detail = NewDetail(record, field, newId);
                        SetNewValue(detail, field, field.Property.GetValue(operation.Entity), lookupNames);
                        record.Details[field.Name] = detail;
                    }

                    buffer.Add(createKey, record);
                    break;
                }
                case DataOperation.Update when operation.FoldIntoCreate:
                {
                    // The same user inserted the row in this transaction: its Create record carries the final
                    // values of the fields this update wrote
                    var record = buffer.Latest(createKey);
                    if (record == null)
                    {
                        break;
                    }

                    if (operation.NameWritten)
                    {
                        record.History.Name = RecordName(operation.Entity);
                    }

                    foreach (var field in FoldedFields(operation))
                    {
                        if (record.Details.TryGetValue(field.Name, out var detail))
                        {
                            SetNewValue(detail, field, field.Property.GetValue(operation.Entity), lookupNames);
                        }
                    }

                    break;
                }
                case DataOperation.Update:
                {
                    if (operation.PreImage == null || operation.ChangedFields.Count == 0)
                    {
                        break;
                    }

                    // Repeated updates by the same user keep the first old value and the latest new value; a
                    // different user starts a new record so each change stays attributed to whoever made it
                    var updateKey = RecordKey(DataOperation.Update, entityKey);
                    var record = buffer.Latest(updateKey);
                    if (record == null || record.ActorId != actorId)
                    {
                        record = await NewRecord(operation, DataOperation.Update, id, operation.PreImage.Name);
                        buffer.Add(updateKey, record);
                    }

                    if (operation.NameWritten)
                    {
                        record.History.Name = RecordName(operation.Entity);
                    }

                    foreach (var field in operation.ChangedFields)
                    {
                        var newValue = field.Property.GetValue(operation.Entity);
                        if (!record.Values.TryGetValue(field.Name, out var values))
                        {
                            var oldValue = operation.PreImage[field];
                            var detail = NewDetail(record, field, newId);
                            SetOldValue(detail, field, oldValue, lookupNames);
                            record.Details[field.Name] = detail;
                            values = (oldValue, newValue);
                        }

                        // Compare the untruncated values: a field that changed back leaves nothing to record
                        if (ValuesEqual(values.Old, newValue))
                        {
                            record.Details.Remove(field.Name);
                            record.Values.Remove(field.Name);
                            continue;
                        }

                        record.Values[field.Name] = (values.Old, newValue);
                        SetNewValue(record.Details[field.Name], field, newValue, lookupNames);
                    }

                    break;
                }
                case DataOperation.Delete:
                {
                    if (operation.PreImage == null)
                    {
                        break;
                    }

                    var record = await NewRecord(operation, DataOperation.Delete, id, operation.PreImage.Name);
                    foreach (var field in operation.Shape.Fields
                                 .Where(x => IsFieldEnabled(operation.AuditInfo, x, DataOperation.Delete)))
                    {
                        var detail = NewDetail(record, field, newId);
                        SetOldValue(detail, field, operation.PreImage[field], lookupNames);
                        record.Details[field.Name] = detail;
                    }

                    buffer.Add(RecordKey(DataOperation.Delete, entityKey), record);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// The create-audited fields an update to a row created in the same transaction wrote, including values the
    /// database generated.
    /// </summary>
    private static IEnumerable<FieldShape> FoldedFields(PendingOperation operation)
    {
        return operation.Shape.Fields
            .Where(x => x.GeneratedOnUpdate || operation.ModifiedProperties.Contains(x.Name))
            .Where(x => IsFieldEnabled(operation.AuditInfo, x, DataOperation.Create));
    }

    private static string RecordName(object entity)
    {
        return (entity.GetNameFieldValue() ?? string.Empty).Truncate(NameMaxLength)!;
    }

    private static Guid? ActorId(IXamsDbContext db)
    {
        return db.GetDataService()?.GetExecutionUserId();
    }

    private static AuditHistoryDetail NewDetail(AuditRecord record, FieldShape field, Func<Guid> newId)
    {
        return new AuditHistoryDetail
        {
            AuditHistoryDetailId = newId(),
            AuditHistoryId = record.History.AuditHistoryId,
            FieldName = field.Name,
            FieldType = field.FieldType,
            TableName = field.LookupTableName
        };
    }

    private static void SetOldValue(AuditHistoryDetail detail, FieldShape field, object? value,
        Dictionary<(Type, object), string?> lookupNames)
    {
        detail.OldValueId = field.LookupType != null ? value as Guid? : null;
        detail.OldValue = FormatFieldValue(field, value, lookupNames);
    }

    private static void SetNewValue(AuditHistoryDetail detail, FieldShape field, object? value,
        Dictionary<(Type, object), string?> lookupNames)
    {
        detail.NewValueId = field.LookupType != null ? value as Guid? : null;
        detail.NewValue = FormatFieldValue(field, value, lookupNames);
    }

    private static string? FormatFieldValue(FieldShape field, object? value,
        Dictionary<(Type, object), string?> lookupNames)
    {
        if (value == null)
        {
            return null;
        }

        var formatted = field.LookupType != null
            ? lookupNames.GetValueOrDefault((field.LookupType, value))
            : FormatValue(value);
        return formatted.Truncate(ValueMaxLength);
    }

    private static bool IsFieldEnabled(Cache.AuditInfo auditInfo, FieldShape field, DataOperation dataOperation)
    {
        if (!auditInfo.FieldAuditInfos.TryGetValue(field.Name, out var fieldAuditInfo))
        {
            return false;
        }

        return dataOperation switch
        {
            DataOperation.Create => fieldAuditInfo.IsCreateAuditEnabled,
            DataOperation.Update => fieldAuditInfo.IsUpdateAuditEnabled,
            DataOperation.Delete => fieldAuditInfo.IsDeleteAuditEnabled,
            _ => false
        };
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (left is byte[] leftBytes && right is byte[] rightBytes)
        {
            return leftBytes.AsSpan().SequenceEqual(rightBytes);
        }

        return Equals(left, right);
    }

    /// <summary>
    /// The acting user and their current name. A user id without a User row cannot be referenced, so it is
    /// kept as the name instead of failing the audited change.
    /// </summary>
    private static async Task<AuditUser> GetUserAsync(IXamsDbContext db, AuditBuffer buffer, Guid userId,
        bool async, CancellationToken cancellationToken)
    {
        if (!buffer.Users.TryGetValue(userId, out var user))
        {
            var query = db.UsersBase.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new { x.Name });
            var stored = async ? await query.FirstOrDefaultAsync(cancellationToken) : query.FirstOrDefault();
            user = stored != null ? new AuditUser(userId, stored.Name) : new AuditUser(null, userId.ToString());
            buffer.Users[userId] = user;
        }

        return user;
    }

    /// <summary>
    /// Reads rows by primary key without tracking, so tracked (modified or deleted) instances do not mask the
    /// stored values. With columns, each row is an array of the key followed by those columns; without, the
    /// whole entity.
    /// </summary>
    private static async Task<Dictionary<object, object>> FindAsync(IXamsDbContext db, Type entityType,
        PropertyInfo keyProperty, PropertyInfo[]? columns, List<object> ids, bool async,
        CancellationToken cancellationToken)
    {
        var rows = new Dictionary<object, object>();
        if (ids.Count == 0)
        {
            return rows;
        }

        var find = Finders.GetOrAdd((entityType, keyProperty.PropertyType),
            x => FindTypedMethod.MakeGenericMethod(x.Entity, x.Key).CreateDelegate<FindRows>());
        foreach (var batch in ids.Chunk(QueryBatchSize))
        {
            foreach (var row in await find(db, keyProperty, columns, batch, async, cancellationToken))
            {
                var id = columns == null ? keyProperty.GetValue(row) : ((object?[])row)[0];
                if (id != null)
                {
                    rows[id] = row;
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// Reads the rows whose key is one of the ids. The ids are read from a field instead of being inlined, so EF
    /// sends them as a single parameter and compiles the query once per entity type and column set rather than
    /// once per save.
    /// </summary>
    private static async Task<List<object>> FindTypedAsync<TEntity, TKey>(IXamsDbContext db,
        PropertyInfo keyProperty, PropertyInfo[]? columns, object[] ids, bool async,
        CancellationToken cancellationToken) where TEntity : class
    {
        var keys = new KeyList<TKey>(ids.Select(ToKey<TKey>).ToList());
        var entity = Expression.Parameter(typeof(TEntity), "x");
        var predicate = Expression.Lambda<Func<TEntity, bool>>(
            Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(TKey)],
                Expression.Field(Expression.Constant(keys), nameof(KeyList<TKey>.Keys)),
                Expression.Property(entity, keyProperty)),
            entity);
        var query = ((DbContext)db).Set<TEntity>().AsNoTracking().Where(predicate);
        if (columns == null)
        {
            var entities = async ? await query.ToListAsync(cancellationToken) : query.ToList();
            return entities.Cast<object>().ToList();
        }

        var selector = Expression.Lambda<Func<TEntity, object?[]>>(
            Expression.NewArrayInit(typeof(object), columns.Prepend(keyProperty)
                .Select(x => (Expression)Expression.Convert(Expression.Property(entity, x), typeof(object)))),
            entity);
        var projected = query.Select(selector);
        var values = async ? await projected.ToListAsync(cancellationToken) : projected.ToList();
        return values.Cast<object>().ToList();
    }

    private static TKey ToKey<TKey>(object id)
    {
        return id is TKey key
            ? key
            : (TKey)Convert.ChangeType(id, Nullable.GetUnderlyingType(typeof(TKey)) ?? typeof(TKey),
                CultureInfo.InvariantCulture);
    }

    private sealed class KeyList<TKey>(List<TKey> keys)
    {
        public readonly List<TKey> Keys = keys;
    }

    private static EntityShape? GetShape(IXamsDbContext db, Type entityType, Cache.MetadataInfo metadata)
    {
        var modelEntityType = db.Model.FindEntityType(entityType);
        if (modelEntityType == null)
        {
            return null;
        }

        return Shapes.GetOrAdd(modelEntityType, x => BuildShape(x, metadata));
    }

    private static EntityShape BuildShape(IEntityType entityType, Cache.MetadataInfo metadata)
    {
        // Single-column foreign keys to Xams tables are lookups; their audit values are the target's name
        var lookups = new Dictionary<string, Cache.MetadataInfo>();
        foreach (var foreignKey in entityType.GetForeignKeys().Where(x => x.Properties.Count == 1))
        {
            if (Cache.Instance.TableTypeMetadata.TryGetValue(foreignKey.PrincipalEntityType.ClrType,
                    out var lookupMetadata))
            {
                lookups.TryAdd(foreignKey.Properties[0].Name, lookupMetadata);
            }
        }

        var fields = entityType.GetProperties()
            .Where(x => x.PropertyInfo != null)
            .Select(x =>
            {
                lookups.TryGetValue(x.Name, out var lookupMetadata);
                return new FieldShape
                {
                    Name = x.Name,
                    Property = x.PropertyInfo!,
                    GeneratedOnUpdate = x.ValueGenerated.HasFlag(ValueGenerated.OnUpdate) ||
                                        x.ValueGenerated.HasFlag(ValueGenerated.OnUpdateSometimes),
                    FieldType = (lookupMetadata != null
                        ? "Lookup"
                        : (Nullable.GetUnderlyingType(x.ClrType) ?? x.ClrType).Name).Truncate(FieldTypeMaxLength)!,
                    LookupType = lookupMetadata?.Type,
                    LookupKeyProperty = lookupMetadata?.PrimaryKeyProperty,
                    LookupTableName = lookupMetadata?.TableName,
                    LookupNameColumns = lookupMetadata == null ? [] : NameColumns(entityType.Model, lookupMetadata)
                };
            })
            .ToList();

        return new EntityShape
        {
            TableName = metadata.TableName,
            KeyProperty = metadata.PrimaryKeyProperty,
            NameProperty = metadata.NameProperty?.Name,
            Fields = fields
        };
    }

    /// <summary>
    /// The columns a lookup's name is read from: none when the table has no name, or null when the name is a
    /// property without a column and the whole row must be read.
    /// </summary>
    private static PropertyInfo[]? NameColumns(IModel model, Cache.MetadataInfo lookupMetadata)
    {
        if (lookupMetadata.NameProperty is not { } nameProperty)
        {
            return [];
        }

        return model.FindEntityType(lookupMetadata.Type)?.FindProperty(nameProperty.Name) != null
            ? [nameProperty]
            : null;
    }

    private static string EntityKey(EntityShape shape, object id)
    {
        return $"{shape.TableName}|{FormatId(id)}";
    }

    private static string RecordKey(DataOperation dataOperation, string entityKey)
    {
        return $"{dataOperation}|{entityKey}";
    }

    private static string FormatId(object id)
    {
        return Convert.ToString(id, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>
    /// What a save writes: its audited operations, the entity types it writes (audited or not), whose cached lookup
    /// names it makes stale, and the names it writes for entities whose history is waiting to be renamed.
    /// </summary>
    private sealed class PendingSave
    {
        public List<PendingOperation> Operations { get; } = [];
        public HashSet<Type> WrittenTypes { get; } = [];
        public List<AuditRename> NameWrites { get; } = [];

        public bool Writes(Type type)
        {
            return WrittenTypes.Any(type.IsAssignableFrom);
        }

        public async Task<int> SaveAsync(AuditBuffer buffer, Func<Task<int>> save)
        {
            int result;
            try
            {
                result = await save();
            }
            finally
            {
                buffer.ForgetLookupNames(WrittenTypes);
            }

            foreach (var rename in NameWrites)
            {
                buffer.Rename(rename.TableName, rename.EntityId, rename.Name);
            }

            return result;
        }
    }

    private sealed class PendingOperation
    {
        public required object Entity { get; init; }
        public required EntityShape Shape { get; init; }
        public required Cache.AuditInfo AuditInfo { get; init; }
        public required DataOperation Operation { get; init; }
        public bool FoldIntoCreate { get; set; }
        public HashSet<string> ModifiedProperties { get; set; } = [];
        public StoredRow? PreImage { get; set; }
        public List<FieldShape> ChangedFields { get; set; } = [];

        /// <summary>
        /// Whether this update writes the record's display name; otherwise the stored name still applies.
        /// </summary>
        public bool NameWritten => Shape.NameProperty != null &&
                                   (ModifiedProperties.Contains(Shape.NameProperty) ||
                                    Shape.Fields.Any(x => x.Name == Shape.NameProperty && x.GeneratedOnUpdate));

        /// <summary>
        /// Whether this update writes a field its record audits: a modified field or one the database generates on
        /// update, enabled for update, or for create when it folds into a Create record (which also takes a new
        /// name).
        /// </summary>
        public bool WritesAuditedField()
        {
            var written = Shape.Fields.Where(x => x.GeneratedOnUpdate || ModifiedProperties.Contains(x.Name));
            return FoldIntoCreate
                ? AuditInfo.IsCreateAuditEnabled &&
                  (NameWritten || written.Any(x => IsFieldEnabled(AuditInfo, x, DataOperation.Create)))
                : AuditInfo.IsUpdateAuditEnabled && written.Any(x => IsFieldEnabled(AuditInfo, x, DataOperation.Update));
        }
    }

    /// <summary>
    /// A row's stored values before the save: the columns read for it, or a whole entity when the record name
    /// comes from a property without a column.
    /// </summary>
    private sealed class StoredRow
    {
        private readonly Func<FieldShape, object?> _value;

        private StoredRow(Func<FieldShape, object?> value, string name)
        {
            _value = value;
            Name = name;
        }

        /// <summary>
        /// The record name the row had.
        /// </summary>
        public string Name { get; }

        public object? this[FieldShape field] => _value(field);

        public static StoredRow FromEntity(object entity)
        {
            return new StoredRow(x => x.Property.GetValue(entity), RecordName(entity));
        }

        /// <summary>
        /// A row read as its key followed by the columns.
        /// </summary>
        public static StoredRow FromColumns(IReadOnlyList<FieldShape> columns, object?[] values, string? nameProperty)
        {
            var byName = new Dictionary<string, object?>(columns.Count);
            for (var i = 0; i < columns.Count; i++)
            {
                byName[columns[i].Name] = values[i + 1];
            }

            // As GetNameFieldValue reads it from an entity
            var name = nameProperty == null ? string.Empty : byName.GetValueOrDefault(nameProperty) as string;
            return new StoredRow(x => byName[x.Name], (name ?? string.Empty).Truncate(NameMaxLength)!);
        }
    }

    private sealed class EntityShape
    {
        public required string TableName { get; init; }
        public required PropertyInfo KeyProperty { get; init; }
        public string? NameProperty { get; init; }
        public required List<FieldShape> Fields { get; init; }

        public object GetId(object entity)
        {
            return KeyProperty.GetValue(entity) ??
                   throw new InvalidOperationException($"{TableName} entity has no primary key value.");
        }

        /// <summary>
        /// The columns old values are read from: the fields audited for the operations and the name. Null when the
        /// name is a property without a column, so the whole row is read.
        /// </summary>
        public IReadOnlyList<FieldShape>? StoredColumns(Cache.AuditInfo auditInfo,
            IEnumerable<DataOperation> operations)
        {
            if (NameProperty != null && Fields.All(x => x.Name != NameProperty))
            {
                return null;
            }

            var audited = operations.ToList();
            return Fields
                .Where(x => x.Name == NameProperty || audited.Any(operation => IsFieldEnabled(auditInfo, x, operation)))
                .ToList();
        }
    }

    private sealed class FieldShape
    {
        public required string Name { get; init; }
        public required PropertyInfo Property { get; init; }
        public required string FieldType { get; init; }
        public bool GeneratedOnUpdate { get; init; }
        public Type? LookupType { get; init; }
        public PropertyInfo? LookupKeyProperty { get; init; }
        public string? LookupTableName { get; init; }

        /// <summary>
        /// The lookup's name columns; null when the whole lookup row must be read.
        /// </summary>
        public PropertyInfo[]? LookupNameColumns { get; init; } = [];
    }
}
