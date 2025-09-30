using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// If the Name property is changed, this service will update all Audit History records to reflect the new name.
/// </summary>
[ServiceLogic("*", DataOperation.Update, LogicStage.PreOperation, int.MaxValue)]
public class AuditNameService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        // Make sure this entity has a Name property
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var nameProperty = metadata.NameProperty;
        if (nameProperty == null)
        {
            return ServiceResult.Success();
        }

        if (context.ValueChanged(nameof(nameProperty.Name)))
        {
            var db = context.GetDbContext<IXamsDbContext>();
            var entity = context.GetEntity<object>();
            string name = entity.GetNameFieldValue() ?? "";
            object id = entity.GetId();
            // On change of the Name attribute of an entity update its name on all the audit history records
            await db.AuditHistoriesBase
                .Where(x => x.TableName == context.TableName && x.EntityId == Convert.ToString(id))
                .ExecuteUpdateAsync(x => 
                    x.SetProperty(y => y.Name, name));
        }

        return ServiceResult.Success();
    }
}
