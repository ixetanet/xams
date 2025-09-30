using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Entities;
using Xams.Core.Interfaces;
using Xams.Core.Utils;
using static Xams.Core.Attributes.DataOperation;

namespace Xams.Core.Services.Auditing;

[ServiceLogic(nameof(AuditHistoryDetail), Create | Update | Delete, LogicStage.PreOperation, Int32.MinValue)]
public class AuditHistoryDetailService : IServiceLogic
{
    public Task<Response<object?>> Execute(ServiceContext context)
    {
        return Task.FromResult(ServiceResult.Error("Cannot Create, Update or Delete Audit History Details"));
    }
}