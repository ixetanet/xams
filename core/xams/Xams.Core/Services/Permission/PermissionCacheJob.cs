using Microsoft.Extensions.Logging;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Permission;

[ServiceJob(nameof(PermissionCacheJob), "System-PermissionCache", "00:00:05", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class PermissionCacheJob : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        var db = context.GetDbContext<BaseDbContext>();
        
        await Permissions.RefreshCache(db, context.Logger);
        
        return ServiceResult.Success();
    }
}