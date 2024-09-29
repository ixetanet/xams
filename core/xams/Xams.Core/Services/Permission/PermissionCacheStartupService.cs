using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Permission;

[ServiceStartup(StartupOperation.Post)]
public class PermissionCacheStartupService : IServiceStartup
{
    public async Task<Response<object?>> Execute(StartupContext startupContext)
    {
        var db = startupContext.DataService.GetDbContext<BaseDbContext>();

        await Permissions.RefreshCache(db, startupContext.DataService.GetLogger());

        return ServiceResult.Success();
    }
}