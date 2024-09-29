using Xams.Core.Dtos;
using Xams.Core.Pipeline;

namespace Xams.Core.Interfaces;

public interface IPipelineStage
{
    public Task<Response<object?>> Execute(PipelineContext context);
    public void SetNext(IPipelineStage nextItem);
}