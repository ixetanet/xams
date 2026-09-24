using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// AuditField records are created and removed by the system. Only their audit flags can be changed.
/// </summary>
[ServiceLogic("AuditField", DataOperation.Create | DataOperation.Update | DataOperation.Delete,
    LogicStage.PreOperation)]
public class AuditFieldService : IServiceLogic
{
    public Task<Response<object?>> Execute(ServiceContext context)
    {
        if (context.DataOperation is DataOperation.Create)
        {
            return Task.FromResult(
                ServiceResult.Error("AuditField records are managed by the system and cannot be created"));
        }

        if (context.DataOperation is DataOperation.Delete)
        {
            return Task.FromResult(
                ServiceResult.Error("AuditField records are managed by the system and cannot be deleted"));
        }

        if (context.ValueChanged("Name") || context.ValueChanged("AuditId"))
        {
            return Task.FromResult(
                ServiceResult.Error("AuditField name and audit cannot be modified"));
        }

        return Task.FromResult(ServiceResult.Success());
    }
}
