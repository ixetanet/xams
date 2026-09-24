using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Entities;
using Xams.Core.Interfaces;
using Xams.Core.Utils;
using static Xams.Core.Attributes.DataOperation;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// Audit history is written by the audit system and purged by the retention job, which do not use the
/// pipeline. It cannot be created, changed or deleted through the API or service logic.
/// </summary>
[ServiceLogic(nameof(AuditHistory), Create | Update | Delete, LogicStage.PreOperation, Int32.MinValue)]
public class AuditHistoryService : IServiceLogic
{
    public Task<Response<object?>> Execute(ServiceContext context)
    {
        return Task.FromResult(ServiceResult.Error("Cannot Create, Update or Delete Audit History"));
    }
}
