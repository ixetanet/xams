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
        // TODO: Add support for numeric id primary key
        if (nameProperty == null || metadata.PrimaryKeyProperty.PropertyType != typeof(Guid))
        {
            return ServiceResult.Success();
        }

        if (context.ValueChanged(nameof(nameProperty.Name)))
        {
            var db = context.GetDbContext<IXamsDbContext>();
            var entity = context.GetEntity<object>();
            string name = entity.GetNameFieldValue(metadata.Type) ?? "";
            object id = entity.GetId();
            // On change of the Name attribute of an entity update it's name on all of the audit history records
            string schema = db.Model.FindEntityType(entity.GetType())?.GetSchema() ?? string.Empty;
            schema = !string.IsNullOrEmpty(schema) ? $"\"{schema}\"." : "";
            string sql = $"UPDATE {schema}\"AuditHistory\"\nSET \"Name\" = @name\nWHERE \"TableName\" = @tableName AND \"EntityId\" = @entityId";
            var parameters = new Dictionary<string, object?>()
            {
                {"name", name},
                {"tableName", context.TableName},
                {"entityId", id}
            };
            await db.ExecuteRawSql(sql,parameters);
        }

        return ServiceResult.Success();
    }
}
