using Xams.Core.Base;
using Xams.Core.Interfaces;

namespace Xams.Core.Contexts;

public class PermissionContext
{
    private IDataService DataService { get; set; }
    public PermissionContext(IDataService dataService)
    {
        DataService = dataService;
    }
    
    public T GetDbContext<T>() where T : BaseDbContext
    {
        return DataService.GetDataRepository().GetDbContext<T>();
    }
    
    public T CreateNewDbContext<T>() where T : BaseDbContext
    {
        return DataService.GetDataRepository().CreateNewDbContext<T>();
    }
    
}