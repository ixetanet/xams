using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

[ServiceLogic("Audit", DataOperation.Delete | DataOperation.Create | DataOperation.Update,
    LogicStage.PreOperation, int.MaxValue)]
internal class AuditService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        if (context.LogicStage is LogicStage.PreOperation)
        {
            if (context.DataOperation is DataOperation.Create)
            {
                return ServiceResult.Error($"Audit records are managed by the system and cannot be created.");
            }

            if (context.DataOperation is DataOperation.Delete)
            {
                return ServiceResult.Error($"Audit records are managed by the system and cannot be deleted.");
            }

            if (context.DataOperation is DataOperation.Update)
            {
                if (context.ValueChanged("Name"))
                {
                    return ServiceResult.Error("Audit table name cannot be modified.");
                }

                if (context.ValueChanged("IsTable"))
                {
                    return ServiceResult.Error("Audit table flag cannot be modified.");
                }
            }
        }
        
        return ServiceResult.Success();
    }
}