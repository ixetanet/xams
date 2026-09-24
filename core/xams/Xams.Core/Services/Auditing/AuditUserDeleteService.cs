using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Entities;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// Deleting a user clears AuditHistory.UserId. History written before UserName existed keeps its attribution by
/// copying the user's name onto it first.
/// </summary>
[ServiceLogic(nameof(User), DataOperation.Delete, LogicStage.PreOperation)]
public class AuditUserDeleteService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        if (context.Entity is not User user)
        {
            return ServiceResult.Success();
        }

        var userName = (user.Name ?? user.UserId.ToString()).Truncate(AuditLogic.UserNameMaxLength);
        await context.GetDbContext<IXamsDbContext>().AuditHistoriesBase
            .Where(x => x.UserId == user.UserId && x.UserName == null)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.UserName, userName));

        return ServiceResult.Success();
    }
}
