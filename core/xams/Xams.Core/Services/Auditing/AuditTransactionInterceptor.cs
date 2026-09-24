using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xams.Core.Base;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// Publishes audit records written inside a transaction the caller opened on the DbContext once that transaction
/// commits, and drops them when it rolls back or fails. XamsDbContext registers it in OnConfiguring.
/// </summary>
internal sealed class AuditTransactionInterceptor : DbTransactionInterceptor
{
    public static readonly AuditTransactionInterceptor Instance = new();

    private AuditTransactionInterceptor()
    {
    }

    public override DbTransaction TransactionStarted(DbConnection connection, TransactionEndEventData eventData,
        DbTransaction result)
    {
        Drop(eventData);
        return result;
    }

    public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection,
        TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
    {
        Drop(eventData);
        return ValueTask.FromResult(result);
    }

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        if (eventData.Context is IXamsDbContext db)
        {
            // Run on the thread pool so blocking cannot deadlock on a caller's synchronization context
            Task.Run(() => AuditLogic.PublishAsync(db, db.GetAuditBuffer().TakeAwaitingCommit(eventData.TransactionId)))
                .GetAwaiter().GetResult();
        }
    }

    public override async Task TransactionCommittedAsync(DbTransaction transaction,
        TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is IXamsDbContext db)
        {
            await AuditLogic.PublishAsync(db, db.GetAuditBuffer().TakeAwaitingCommit(eventData.TransactionId));
        }
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        Drop(eventData);
    }

    public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Drop(eventData);
        return Task.CompletedTask;
    }

    public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
    {
        Drop(eventData);
    }

    public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Drop(eventData);
        return Task.CompletedTask;
    }

    private static void Drop(TransactionEventData eventData)
    {
        (eventData.Context as IXamsDbContext)?.GetAuditBuffer().TakeAwaitingCommit(Guid.Empty);
    }
}
