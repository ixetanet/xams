using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
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
        var db = context.GetDbContext<IXamsDbContext>();
        
        var retentionDays = int.Parse(await Queries.GetCreateSetting(db, JobStartupService.SettingName, "30") ?? "30");

        await db.JobHistoriesBase
            .Where("CreatedDate < @0", DateTime.UtcNow.AddDays(-retentionDays))
            .ExecuteDeleteAsync();
        
        return ServiceResult.Success();
    }
}