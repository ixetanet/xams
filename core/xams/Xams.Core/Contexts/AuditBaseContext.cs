using Xams.Core.Base;
using Xams.Core.Interfaces;

namespace Xams.Core.Contexts;

public class AuditBaseContext
{
    public IXamsDbContext XamsDbContext { get; internal set; } = null!;
    /// <summary>
    /// The DataService that made the changes, or null when the DbContext was not created by Xams
    /// (for example a DbContext resolved from dependency injection).
    /// </summary>
    public IDataService? DataService { get; internal set; }

    public T GetDbContext<T>() where T : IXamsDbContext
    {
        return (T)XamsDbContext;
    }
}
