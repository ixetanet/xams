using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Pipeline.Stages.Shared;
using Xams.Core.Repositories;

namespace Xams.Core.Pipeline.Stages;

public class PipeEntityRead : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var readResponse = await context.DataRepository.Read(context.UserId, context.ReadInput, new ReadOptions()
        {
            Permissions = context.Permissions
        });
        
        if (readResponse.Succeeded)
        {
            context.Entities = readResponse.Data.results;
            context.ReadOutput = readResponse.Data;
            List<object> results =
                await AppendUIInfo.Set(context, context.ReadOutput);
            results = AppendOutputParameters.Set(context, results);
            context.ReadOutput.results = results;
            context.ServiceContext.ReadOutput = context.ReadOutput;
        }
        else
        {
            return new Response<object?>()
            {
                Succeeded = readResponse.Succeeded,
                FriendlyMessage = readResponse.FriendlyMessage,
                LogMessage = readResponse.LogMessage,
            };
        }
        return await base.Execute(context);
    }
}