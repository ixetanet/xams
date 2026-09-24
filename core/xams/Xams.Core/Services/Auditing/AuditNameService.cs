using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// If the name property is changed, this service will update all Audit History records to reflect the new name.
/// </summary>
/// <remarks>
/// Inside a DataRepository transaction the renames are applied together just before the commit.
/// </remarks>
[ServiceLogic("*", DataOperation.Update, LogicStage.PreOperation, int.MaxValue)]
public class AuditNameService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        // Make sure this entity has a name property
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var nameProperty = metadata.NameProperty;
        if (nameProperty == null)
        {
            return ServiceResult.Success();
        }

        if (context.ValueChanged(nameProperty.Name))
        {
            var db = context.GetDbContext<IXamsDbContext>();
            var entity = context.GetEntity<object>();
            var name = (entity.GetNameFieldValue() ?? "").Truncate(AuditLogic.NameMaxLength)!;
            var id = Convert.ToString(entity.GetId(), System.Globalization.CultureInfo.InvariantCulture)!;
            // On change of the name of an entity update its name on all the audit history records
            await AuditHistoryNames.RenameAsync(db, context.TableName, id, name);
        }

        return ServiceResult.Success();
    }
}
