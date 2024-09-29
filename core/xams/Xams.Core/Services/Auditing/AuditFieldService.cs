using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

[ServiceLogic("AuditField", DataOperation.Delete, LogicStage.PreOperation)]
public class AuditFieldService : IServiceLogic
{
    public Task<Response<object?>> Execute(ServiceContext context)
    {
        return Task.FromResult(ServiceResult.Error("AuditField records are managed by the system and cannot be deleted"));
    }
}