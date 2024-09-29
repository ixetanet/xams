using Xams.Core.Contexts;
using Xams.Core.Dtos;

namespace Xams.Core.Interfaces;

/// <summary>
/// Execute a service action.
/// </summary>
public interface IServiceAction
{
    public Task<Response<object?>> Execute(ActionServiceContext context);
}