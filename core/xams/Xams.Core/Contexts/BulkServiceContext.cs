using Xams.Core.Base;
using Xams.Core.Interfaces;
using Xams.Core.Pipeline;

namespace Xams.Core.Contexts;

public class BulkServiceContext : BaseServiceContext
{
    // Every ServiceContext that was created during the transaction
    // ie: Child ServiceContexts
    public List<ServiceContext> AllServiceContexts { get; private set; }
    
    public BulkServiceContext(PipelineContext pipelineContext, List<ServiceContext> allServiceContexts) : base(pipelineContext)
    {
        AllServiceContexts = allServiceContexts;
    }
}