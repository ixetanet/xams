using System.Text.Json;
using Xams.Core.Base;
using Xams.Core.Interfaces;
using Xams.Core.Pipeline;
using Xams.Core.Repositories;

namespace Xams.Core.Contexts;

public class JobServiceContext : BaseServiceContext
{
    public JobServiceContext(PipelineContext pipelineContext) : base(pipelineContext)
    {
    }
}