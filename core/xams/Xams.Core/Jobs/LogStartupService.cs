using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Jobs;

// Set order to 101 to ensure it runs after JobStartupService
[ServiceStartup(StartupOperation.Post, 101)]
public class LogStartupService : IServiceStartup
{
    public static string SettingName = "LOG_HISTORY_RETENTION_DAYS";

    public async Task<Response<object?>> Execute(StartupContext startupContext)
    {
        var db = startupContext.DataService.GetDbContext<IXamsDbContext>();
        await Queries.GetCreateSetting(db, SettingName, "30");
        return ServiceResult.Success();
    }
}