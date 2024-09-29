using Xams.Core.Base;
using Xams.Core.Dtos;

namespace Xams.Core.Pipeline.Stages;

public class PipeResultReadOutput : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        return new Response<object?>()
        {
            Succeeded = true,
            Data =  context.ReadOutput
        };
    }
}