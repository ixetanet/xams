using Xams.Core.Base;
using Xams.Core.Dtos;

namespace Xams.Core.Pipeline.Stages;

public class PipeAddEntityToEntities : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        context.Entities = new List<object>();
        // Handle Create\Update\Delete entity for service logic
        if (context.Entity != null)
        {
            context.Entities.Add(context.Entity);
        }
        return await base.Execute(context);
    }
}