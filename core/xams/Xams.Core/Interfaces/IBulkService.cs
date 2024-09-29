using Xams.Core.Contexts;
using Xams.Core.Dtos;

namespace Xams.Core.Interfaces;

public interface IBulkService
{
    public Task<Response<object?>> Execute(BulkServiceContext context);
}