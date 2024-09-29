using Xams.Core.Interfaces;

namespace Xams.Core.Contexts;

public class StartupContext
{
    public IServiceProvider ServiceProvider { get; }
    public IDataService DataService { get; }
    
    public StartupContext(IServiceProvider serviceProvider, IDataService dataService)
    {
        ServiceProvider = serviceProvider;
        DataService = dataService;
    }
    
}