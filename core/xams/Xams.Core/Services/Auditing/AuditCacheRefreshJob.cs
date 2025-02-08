using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

[JobServer(ExecuteJobOn.One)]
[ServiceJob(nameof(AuditCacheRefreshJob), "System", "00:05:00", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class AuditCacheRefreshJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        if (!Cache.Instance.IsAuditEnabled)
        {
            return ServiceResult.Success();
        }
        
        Console.WriteLine("Refreshing Audit Records Cache");
        await AuditStartupService.CacheAuditRecords(context.GetDbContext<BaseDbContext>());

        return ServiceResult.Success();
    }
}