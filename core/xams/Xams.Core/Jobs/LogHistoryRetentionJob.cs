using System.Linq.Dynamic.Core;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Jobs;

// Run every 30 minutes and clear the log history
[JobServer(ExecuteJobOn.One)]
[ServiceJob(nameof(LogHistoryRetentionJob), "System-LogHistory", "00:30:00", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class LogHistoryRetentionJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        var db = context.GetDbContext<IXamsDbContext>();
        Type logType = Cache.Instance.GetTableMetadata("Log").Type;
        
        var retentionDays = int.Parse((await Queries.GetCreateSetting(db, LogStartupService.SettingName, "30") ?? "30"));
        
        DynamicLinq dynamicLinq = new DynamicLinq(db, logType);
        var query = dynamicLinq.Query.Where("Timestamp < @0", DateTime.UtcNow.AddDays(-retentionDays));
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