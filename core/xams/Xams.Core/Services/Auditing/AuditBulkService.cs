using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

[BulkService(BulkStage.Post, int.MaxValue)]
public class AuditBulkService : IBulkService
{
    public async Task<Response<object?>> Execute(BulkServiceContext context)
    {
        // If there have been any changes to any audit records, update the
        // System.AuditLastRefresh value to the current time
        // This will cause every server to refresh its audit records in the AuditCacheRefreshJob
        var auditUpdates = context
            .AllServiceContexts
            .Any(x => x.TableName == "Audit" && x.DataOperation != DataOperation.Read);

        var auditFieldUpdates = context
            .AllServiceContexts
            .Any(x => x.TableName == "AuditField" && x.DataOperation != DataOperation.Read);

        if (auditUpdates || auditFieldUpdates)
        {
            var db = context.DataService.GetDbContext<IXamsDbContext>();
            // Written in the same transaction as the change, so a rollback leaves the marker alone
            await Queries.UpdateSystemRecord(db, AuditStartupService.AuditRefreshSystemRecord,
                DateTime.UtcNow.ToString("O"));

            // Refresh this server once the change is committed; a rollback skips the refresh
            var dataRepository = context.DataRepository;
            await dataRepository.AfterCommit(async () =>
            {
                using var refreshDb = dataRepository.CreateNewDbContext();
                await AuditStartupService.CacheAuditRecords(refreshDb);
            });
        }

        return ServiceResult.Success();
    }
}
