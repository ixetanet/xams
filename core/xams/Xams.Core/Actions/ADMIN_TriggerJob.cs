using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Jobs;
using Xams.Core.Utils;

namespace Xams.Core.Actions;

[ServiceAction("ADMIN_TriggerJob")]
public class ADMIN_TriggerJob : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        if (context.Parameters == null)
        {
            return ServiceResult.Error($"Missing parameters");
        }

        if (!context.Parameters.ContainsKey("jobName"))
        {
            return ServiceResult.Error($"Missing parameter jobName");
        }
        
        string? jobName = context.Parameters["jobName"].GetString();

        if (string.IsNullOrEmpty(jobName))
        {
            return ServiceResult.Error($"jobName cannot be null or empty string");
        }
        
        await JobService.Singleton?.ExecuteJob(jobName)!;

        return ServiceResult.Success();
    }
}