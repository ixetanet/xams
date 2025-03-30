using Microsoft.Extensions.Logging;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipeExecuteServiceLogic : BasePipelineStage
{
    private readonly LogicStage _logicStage;
    public PipeExecuteServiceLogic(LogicStage logicStage)
    {
        _logicStage = logicStage;
    }
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var response = await ExecuteServiceLogic(context);
        if (!response.Succeeded)
        {
            return response;
        }

        context.OutputParameters =
            Util.MergeParameters(context.OutputParameters, Util.ObjectToParameters(response.Data)); 
        context.ReadOutput = SetMissingUiInfo(response, context.ReadOutput);
        return await base.Execute(context);
    }
    
    private ReadOutput? SetMissingUiInfo(Response<object?> response, ReadOutput? readResponseData)
    {
        if (response.Data is ReadOutput)
        {
            var result = (ReadOutput)response.Data;
            if (!result.IsUIInfoSet())
            {
                result.SetUIInfo(false, false, false);
            }

            return result;
        }

        return readResponseData;
    }
    
    private async Task<Response<object?>> ExecuteServiceLogic(PipelineContext context)
        {
            List<Type> serviceLogics = new List<Type>();

            if (Cache.Instance.ServiceLogics.TryGetValue(context.TableName, out var tableServiceLogic))
            {
                serviceLogics.AddRange(tableServiceLogic
                    .Where(x => x.ServiceLogicAttribute.LogicStage.HasFlag(_logicStage))
                    .Where(x => x.ServiceLogicAttribute.DataOperation.HasFlag(context.DataOperation))
                    .Select(x => x.Type).ToList());
            }

            if (Cache.Instance.ServiceLogics.TryGetValue("*", out var allServiceLogic))
            {
                serviceLogics.AddRange(allServiceLogic
                    .Where(x => x.ServiceLogicAttribute.LogicStage.HasFlag(_logicStage))
                    .Where(x => x.ServiceLogicAttribute.DataOperation.HasFlag(context.DataOperation))
                    .Select(x => x.Type).ToList());
            }
            

            if (!serviceLogics.Any())
            {
                return new Response<object?>()
                {
                    Succeeded = true
                };
            }
            
            var serviceContext = context.ServiceContext;
            serviceContext.LogicStage = _logicStage;
            serviceContext.Entities = context.Entities ?? new List<object>();
            serviceContext.ReadInput = context.ReadInput;
            serviceContext.ReadOutput = context.ReadOutput;

            object? outputData = null;
            foreach (var serviceLogic in serviceLogics)
            {
                // This is intentionally not wrapped in try catch so the entire
                // stack trace is sent to the browser log
                var instance = Activator.CreateInstance(serviceLogic);
                var executeMethod = serviceLogic.GetMethod("Execute");
                if (executeMethod != null)
                {
                    Response<object?> response = await ((Task<Response<object?>>)executeMethod.Invoke(instance,
                    [
                        serviceContext
                    ])!);
                    
                    if (!response.Succeeded)
                    {
                        return response;
                    }
                    
                    if (context.DataOperation is DataOperation.Read && response.Data is ReadOutput)
                    {
                        outputData = response.Data;
                    }
                    else if (response.Data != null)
                    {
                        outputData = response.Data;
                    }
                }
            }

            return new Response<object?>()
            {
                Succeeded = true,
                Data = outputData
            };
        }
}