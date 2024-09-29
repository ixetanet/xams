using Xams.Core.Contexts;
using Xams.Core.Dtos;

namespace Xams.Core.Interfaces;

/// <summary>
/// Execute a service logic on create, read, update, or delete.
/// </summary>
public interface IServiceLogic 
{
    public Task<Response<object?>> Execute(ServiceContext context);
}
