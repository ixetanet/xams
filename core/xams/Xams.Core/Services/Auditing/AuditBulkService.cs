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
        if (!Cache.Instance.IsAuditEnabled)
        {
            return ServiceResult.Success();
        }
        // If there have been any changes to any audit records, update the 
        // System.AuditLastRefresh value to the current time
        // This will cause the audit records to be refreshed by the AudJobService
        var auditUpdates = context
            .AllServiceContexts
            .Any(x => x.TableName == "Audit" && x.DataOperation != DataOperation.Read);
        
        var auditFieldUpdates = context
            .AllServiceContexts
            .Any(x => x.TableName == "AuditField" && x.DataOperation != DataOperation.Read);
        
        if (auditUpdates || auditFieldUpdates)
        {
            var dataService = context.DataService;
            var db = dataService.GetDbContext<BaseDbContext>();
            await Queries.UpdateSystemRecord(db, "AuditLastRefresh", DateTime.UtcNow.ToString("O"));
            await AuditStartupService.CacheAuditRecords(db);
        }

        return ServiceResult.Success();
    }
}