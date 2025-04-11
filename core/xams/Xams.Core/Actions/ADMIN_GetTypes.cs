using System.Text;
using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

// ReSharper disable InconsistentNaming

namespace Xams.Core.Actions;

/// <summary>
/// Return tables from metadata as typescript types
/// </summary>
[ServiceAction(nameof(ADMIN_GetTypes))]
public class ADMIN_GetTypes : IServiceAction
{
    public Task<Response<object?>> Execute(ActionServiceContext context)
    {
        var sb = new StringBuilder();
        foreach (var key in Cache.Instance.TableMetadata)
        {
            var tableName = key.Key;
            var metadata = key.Value;
            sb.AppendLine($"export type {tableName} = {{");
            foreach (var field in metadata.MetadataOutput.fields)
            {
                var nullable = field.isNullable ? "?" : string.Empty;
                sb.AppendLine($"\t{field.name}{nullable}: {GetFieldType(field)};");
                if (field.type?.ToLower() == "lookup")
                {
                    sb.AppendLine($"\t{field.lookupName}{nullable}: string;");
                }
            }
            
            // Get ICollection properties
            var properties = metadata.Type.GetProperties()
                .Where(x => x.PropertyType.Name == "ICollection`1");

            foreach (var property in properties)
            {
                var entityType = property.PropertyType.GenericTypeArguments[0];
                sb.AppendLine($"\t{property.Name}: {entityType.Metadata().TableName}[]");
            }
            
            
            sb.AppendLine("}");
            sb.AppendLine();
        }
        return Task.FromResult(ServiceResult.Success(sb.ToString()));
    }

    private string GetPrimaryKeyType(Type type)
    {
        if (type == typeof(Guid))
        {
            return "string";
        }
        
        if (type == typeof(bool))
        {
            return "boolean";
        }

        if (type == typeof(char))
        {
            return "string";
        }

        if (type.IsPrimitive)
        {
            return "number";
        }

        return "string";
    }

    private string GetFieldType(MetadataField field)
    {
        switch (field.type?.ToLower())
        {
            case "string":
                return "string";
            case "guid":
                return "string";
            case "lookup":
                return "string";
            case "char":
                return "string";
            case "datetime":
                return "string";
            case "int64":
                return "bigint";
            case "uint64":
                return "bigint";
            case "boolean":
                return "boolean";
            default:
                return "number";
        }
    }
}