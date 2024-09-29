using Xams.Core.Base;
using Xams.Core.Dtos;

namespace Xams.Core.Pipeline.Stages;

public class PipeEntityUpdate : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var response = await context.DataRepository.Update(context.Entity, context.SystemParameters.PreventSave);
        if (!response.Succeeded)
        {
            return response;
        }
        return await base.Execute(context);
    }
}