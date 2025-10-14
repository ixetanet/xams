using Xams.Core.Base;
using Xams.Core.Interfaces;
using Xams.Core.Pipeline;
using Xams.Core.Startup;

namespace Xams.Core.Contexts;

public class StartupContext : BaseServiceContext
{
    public IServiceProvider ServiceProvider { get; }
    public new IDataService DataService => base.DataService;

    public StartupContext(IServiceProvider serviceProvider, IDataService dataService)
        : base(CreatePipelineContext(dataService))
    {
        ServiceProvider = serviceProvider;
    }

    private static PipelineContext CreatePipelineContext(IDataService dataService)
    {
        var pipelineContext = new PipelineContext
        {
            UserId = SystemRecords.SystemUserId,
            DataService = dataService,
            DataRepository = dataService.GetDataRepository(),
            MetadataRepository = dataService.GetMetadataRepository(),
            SecurityRepository = dataService.GetSecurityRepository()
        };

        return pipelineContext;
    }
}