using System.Linq.Dynamic.Core;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Jobs;

// Run every 30 minutes and clear the audit history
[JobServer(ExecuteJobOn.One)]
[ServiceJob(nameof(JobHistoryRetentionJob), "System-JobHistory", "00:30:00", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class JobHistoryRetentionJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        var db = context.GetDbContext<BaseDbContext>();
        Type jobHistoryType = Cache.Instance.GetTableMetadata("JobHistory").Type;
        
        DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(db, jobHistoryType);
        var query = dynamicLinq.Query.Where("CreatedDate < @0", DateTime.UtcNow.AddDays(-Cache.Instance.JobHistoryRetentionDays));
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