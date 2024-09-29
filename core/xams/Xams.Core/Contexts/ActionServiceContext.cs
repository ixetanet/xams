using Microsoft.AspNetCore.Http;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Pipeline;

namespace Xams.Core.Contexts;

public class ActionServiceContext : BaseServiceContext
{
    public DataOperation DataOperation { get => DataOperation.Action; }
    public IFormFile? File { get => PipelineContext.File; }
    
    public ActionServiceContext(PipelineContext pipelineContext) : base(pipelineContext)
    {
        
    }
}