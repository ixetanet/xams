using Xams.Core.Contexts;
using Xams.Core.Dtos;

namespace Xams.Core.Interfaces;

/// <summary>
/// Executes logic on startup.
/// </summary>
public interface IServiceStartup
{
    public Task<Response<object?>> Execute(StartupContext startupContext);
}