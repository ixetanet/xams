using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Xams.Core.Base;
using Xams.Core.Entities;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// Keeps the record name on audit history in line with renames. Inside a DataRepository transaction the renames
/// are collected and applied just before the commit, with one statement per batch of records instead of one per
/// record.
/// </summary>
internal static class AuditHistoryNames
{
    // Records per statement. They are listed in a compound SELECT, and SQLite allows 500 terms in one.
    private const int BatchSize = 200;

    /// <summary>
    /// Renames the record's audit history, or holds the rename until the commit when a DataRepository
    /// transaction is open.
    /// </summary>
    public static Task RenameAsync(IXamsDbContext db, string tableName, string entityId, string name,
        CancellationToken cancellationToken = default)
    {
        var buffer = db.GetAuditBuffer();
        if (buffer.Deferred)
        {
            buffer.Rename(tableName, entityId, name);
            return Task.CompletedTask;
        }

        return ApplyAsync(db, [new AuditRename(tableName, entityId, name)], cancellationToken);
    }

    /// <summary>
    /// Gives records not yet written the final names of their renamed entities.
    /// </summary>
    internal static void Rename(IEnumerable<AuditRecord> records, IReadOnlyList<AuditRename> renames)
    {
        if (renames.Count == 0)
        {
            return;
        }

        var names = renames.ToDictionary(x => (x.TableName, x.EntityId), x => x.Name);
        foreach (var record in records)
        {
            if (record.History.TableName != null && record.History.EntityId != null &&
                names.TryGetValue((record.History.TableName, record.History.EntityId), out var name))
            {
                record.History.Name = name;
            }
        }
    }

    public static async Task ApplyAsync(IXamsDbContext db, IReadOnlyList<AuditRename> renames,
        CancellationToken cancellationToken = default)
    {
        var context = (DbContext)db;
        // Providers whose SQL for a batched rename is known; any other renames record by record
        var batched = context.Database.ProviderName is "Microsoft.EntityFrameworkCore.SqlServer"
            or "Npgsql.EntityFrameworkCore.PostgreSQL" or "Microsoft.EntityFrameworkCore.Sqlite";
        foreach (var table in renames.GroupBy(x => x.TableName))
        {
            foreach (var batch in table.Chunk(BatchSize))
            {
                if (batched)
                {
                    await UpdateBatchAsync(context, table.Key, batch, cancellationToken);
                }
                else
                {
                    await UpdateEachAsync(db, table.Key, batch, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Renames the history of a batch of records in one statement.
    /// </summary>
    private static async Task UpdateBatchAsync(DbContext context, string tableName, AuditRename[] batch,
        CancellationToken cancellationToken)
    {
        var sql = context.GetService<ISqlGenerationHelper>();
        var entityType = context.Model.FindEntityType(typeof(AuditHistory))!;
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        var tableSql = sql.DelimitIdentifier(table.Name, table.Schema);
        IProperty Property(string name) => entityType.FindProperty(name)!;
        string Column(string name) => sql.DelimitIdentifier(Property(name).GetColumnName(table)!);
        var renamed = sql.DelimitIdentifier("Renamed");
        var renamedId = sql.DelimitIdentifier("RenamedId");
        var renamedName = sql.DelimitIdentifier("RenamedName");

        await using var parameterFactory = context.Database.GetDbConnection().CreateCommand();
        var parameters = new List<object>();
        string Parameter(string property, string value)
        {
            var name = "p" + parameters.Count;
            parameters.Add(Property(property).GetRelationalTypeMapping()
                .CreateParameter(parameterFactory, sql.GenerateParameterName(name), value));
            return sql.GenerateParameterNamePlaceholder(name);
        }

        var tableNameParameter = Parameter(nameof(AuditHistory.TableName), tableName);
        var rows = string.Join(" UNION ALL ", batch.Select(x =>
            $"SELECT {Parameter(nameof(AuditHistory.EntityId), x.EntityId)}, " +
            $"{Parameter(nameof(AuditHistory.Name), x.Name)}"));
        var entityId = $"{tableSql}.{Column(nameof(AuditHistory.EntityId))}";
        var text =
            $"WITH {renamed} ({renamedId}, {renamedName}) AS ({rows}) " +
            $"UPDATE {tableSql} SET {Column(nameof(AuditHistory.Name))} = " +
            $"(SELECT {renamed}.{renamedName} FROM {renamed} WHERE {renamed}.{renamedId} = {entityId}) " +
            $"WHERE {tableSql}.{Column(nameof(AuditHistory.TableName))} = {tableNameParameter} " +
            $"AND {entityId} IN (SELECT {renamed}.{renamedId} FROM {renamed})";
        await context.Database.ExecuteSqlRawAsync(text, parameters, cancellationToken);
    }

    /// <summary>
    /// Renames the history of a batch of records one record at a time, skipping records without history.
    /// </summary>
    internal static async Task UpdateEachAsync(IXamsDbContext db, string tableName, AuditRename[] batch,
        CancellationToken cancellationToken)
    {
        var ids = batch.Select(x => x.EntityId).ToList();
        var withHistory = (await db.AuditHistoriesBase
                .Where(x => x.TableName == tableName && ids.Contains(x.EntityId!))
                .Select(x => x.EntityId!)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();
        foreach (var rename in batch.Where(x => withHistory.Contains(x.EntityId)))
        {
            await db.AuditHistoriesBase
                .Where(x => x.TableName == tableName && x.EntityId == rename.EntityId)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.Name, rename.Name), cancellationToken);
        }
    }
}
