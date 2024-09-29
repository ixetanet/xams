using Xams.Core.Base;
using Xams.Core.Dtos;

namespace Xams.Core.Pipeline.Stages;

public class PipeResultEmpty : BasePipelineStage
{
    public override Task<Response<object?>> Execute(PipelineContext context)
    {
        return Task.FromResult(new Response<object?>()
        {
            Succeeded = true
        });
    }
}