using Xams.Core.Base;

namespace Xams.Core.Contexts;

public class SecurityContext
{
    public Guid ExecutingUserId { get; private set; }
    public List<Permissions.PermissionRequest> PermissionRequests { get; private set; }
    private BaseDbContext? _dataContext { get; set; }
    
    public SecurityContext(
        Guid executingUserId,
        BaseDbContext dataContext,
        List<Permissions.PermissionRequest> permissionRequests)
    {
        ExecutingUserId = executingUserId;
        PermissionRequests = permissionRequests;
        _dataContext = dataContext;
    }
    
    
    public TDbContext GetDbContext<TDbContext>() where TDbContext : BaseDbContext
    {
        if (!(_dataContext is TDbContext))
        {
            throw new Exception($"Cannot GetDbContext from SecurityContext, DbContext is not of type {typeof(TDbContext).Name}.");
        }

        return (TDbContext)_dataContext!;
    }
}