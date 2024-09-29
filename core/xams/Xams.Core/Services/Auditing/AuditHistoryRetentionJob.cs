using System.Linq.Dynamic.Core;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

// Run every 30 minutes and clear the audit history
[ServiceJob(nameof(AuditHistoryRetentionJob), "System-AuditHistory", "00:30:00", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class AuditHistoryRetentionJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        if (!Cache.Instance.IsAuditEnabled)
        {
            return ServiceResult.Success();
        }
        
        Console.WriteLine("Deleting Audit History outside of retention period");
        var db = context.GetDbContext<BaseDbContext>();
        Type auditHistoryType = Cache.Instance.GetTableMetadata("AuditHistory").Type;
        
        DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(db, auditHistoryType);
        var query = dynamicLinq.Query.Where("CreatedDate < @0", DateTime.UtcNow.AddDays(-Cache.Instance.AuditHistoryRetentionDays));
        var results = await query.ToDynamicArrayAsync();
        
        foreach (var result in results)
        {
            // Use the DataService to delete the records to ensure no exceptions are thrown
            // for foreign key constraints
            await context.Delete(result);
        }

        return ServiceResult.Success();
    }
}