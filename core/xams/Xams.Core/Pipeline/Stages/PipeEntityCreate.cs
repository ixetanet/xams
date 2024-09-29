using Xams.Core.Base;
using Xams.Core.Dtos;

namespace Xams.Core.Pipeline.Stages;

public class PipeEntityCreate : BasePipelineStage
{
    public override async  Task<Response<object?>> Execute(PipelineContext context)
    {
        var response = await context.DataRepository.Create(context.Entity, context.SystemParameters.PreventSave);
        if (!response.Succeeded)
        {
            return response;
        }
        return await base.Execute(context);
    }
}