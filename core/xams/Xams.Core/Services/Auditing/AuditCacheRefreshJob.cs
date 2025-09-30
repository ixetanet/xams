using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

[JobServer(ExecuteJobOn.One)]
[ServiceJob(nameof(AuditCacheRefreshJob), "System", "00:01:00", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class AuditCacheRefreshJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        Console.WriteLine("Refreshing Audit Records Cache");
        await AuditStartupService.CacheAuditRecords(context.GetDbContext<IXamsDbContext>());

        return ServiceResult.Success();
    }
}