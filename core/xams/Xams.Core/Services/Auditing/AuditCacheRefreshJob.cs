using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

// The audit configuration is cached per server, so every server checks for changes
[JobServer(ExecuteJobOn.All)]
[ServiceJob(nameof(AuditCacheRefreshJob), "System", "00:01:00", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class AuditCacheRefreshJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        return await AuditStartupService.CacheAuditRecords(context.GetDbContext<IXamsDbContext>());
    }
}
