using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Pipeline;

namespace Xams.Core.Base;


public class BasePipelineStage : IPipelineStage
{
    protected IPipelineStage? _next;
    
    public void SetNext(IPipelineStage nextItem)
    {
        _next = nextItem;
    }

    public virtual async Task<Response<object?>> Execute(PipelineContext context)
    {
        if (_next != null)
        {
            var response = await _next.Execute(context);
            return response;
        }

        return new Response<object?> { Succeeded = true };
    }
}


