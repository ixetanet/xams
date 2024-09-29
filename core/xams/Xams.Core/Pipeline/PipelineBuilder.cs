using Xams.Core.Dtos;
using Xams.Core.Interfaces;

namespace Xams.Core.Pipeline;

public class PipelineBuilder
{
    private IPipelineStage? _first;
    private IPipelineStage? _last;
    
    public PipelineBuilder Add(IPipelineStage pipelineStage)
    {
        if (_first == null)
        {
            _first = pipelineStage;
            _last = pipelineStage;
        }
        else
        {
            _last!.SetNext(pipelineStage);
            _last = pipelineStage;
        }
        return this;
    }

    public async Task<Response<object?>> Execute(PipelineContext pipelineContext)
    {
        return await _first?.Execute(pipelineContext)!;
    }
}