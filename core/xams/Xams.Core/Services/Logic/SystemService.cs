using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;
using static Xams.Core.Attributes.DataOperation;
using static Xams.Core.Attributes.LogicStage;

namespace Xams.Core.Services.Logic;

[ServiceLogic("System", Create | Update | Delete, PreOperation)]
public class SystemService : IServiceLogic
{
    public Task<Response<object?>> Execute(ServiceContext context)
    {
        return Task.FromResult(ServiceResult.Error("System records cannot be modified."));
    }
}