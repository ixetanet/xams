using Xams.Core.Contexts;
using Xams.Core.Dtos;

namespace Xams.Core.Interfaces;

public interface IServiceJob
{
    public Task<Response<object?>> Execute(JobServiceContext context);
}