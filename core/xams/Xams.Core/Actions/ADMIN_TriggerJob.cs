using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Jobs;
using Xams.Core.Utils;
// ReSharper disable InconsistentNaming

namespace Xams.Core.Actions;

[ServiceAction("ADMIN_TriggerJob")]
public class ADMIN_TriggerJob : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        if (!context.Parameters.ContainsKey("jobName"))
        {
            return ServiceResult.Error($"Missing parameter jobName");
        }
        
        string? jobName = context.Parameters["jobName"].GetString();

        if (string.IsNullOrEmpty(jobName))
        {
            return ServiceResult.Error($"jobName cannot be null or empty string");
        }
        
        // Create system records to execute job(s)
        if (!Cache.Instance.ServiceJobs.ContainsKey(jobName))
        {
            return ServiceResult.Error($"Job {jobName} not found");
        }

        JsonElement? parameters = null;
        if (context.Parameters.ContainsKey("parameters"))
        {
            parameters = context.Parameters["parameters"];
        }
            
        var jobOptions = new JobOptions
        {
            JobName = jobName,
            Parameters = parameters,
        };
        
        await context.ExecuteJob(jobOptions);

        return ServiceResult.Success();
    }
}
