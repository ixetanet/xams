using System.Reflection;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipeValidateNonNullableProperties : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var nonNullableGuidsResponse = ValidateNonNullableProperties(context);
        if (!nonNullableGuidsResponse.Succeeded)
        {
            return nonNullableGuidsResponse!;
        }
        return await base.Execute(context);
    }
    
    private Response<object> ValidateNonNullableProperties(PipelineContext context)
    {
        NullabilityInfoContext nullabilityInfoContext = new();

        if (context.Entity == null)
        {
            return new Response<object>()
            {
                Succeeded = false,
                FriendlyMessage = "Entity cannot be null."
            };
        }
        
        var properties = context.Entity.GetType().GetProperties();
        foreach (var property in properties)
        {
            // Ignore primary key
            if (context.Entity.Metadata().PrimaryKeyProperty == property)
            {
                continue;
            }
        
            Type? underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
            if (underlyingType == null && property.PropertyType == typeof(Guid))
            {
                Guid value = (Guid)(property.GetValue(context.Entity) ?? Guid.Empty);
                if (value == Guid.Empty)
                {
                    return new Response<object>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"{property.Name} on {context.TableName} cannot be null."
                    };
                }
            }
        }

        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(string))
            {
                var info = nullabilityInfoContext.Create(property);
                var isNullable = info.WriteState == NullabilityState.Nullable;
                if (isNullable)
                {
                    continue;
                }
                
                var value = (string?)property.GetValue(context.Entity);
                if (string.IsNullOrEmpty(value))
                {
                    var metadata = Cache.Instance.GetTableMetadata(context.TableName);
                    var propDisplayName = metadata.MetadataOutput.fields
                        .FirstOrDefault(x => x.name == property.Name)?
                        .displayName;
                    propDisplayName ??= property.Name;
                    var tableDisplayName = metadata.DisplayNameAttribute?.Name ?? context.TableName;
                    return new Response<object>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"{propDisplayName} on {tableDisplayName} is required." ,
                        LogMessage = $"Error validating non-nullable properties while attempting to {context.DataOperation}. {propDisplayName} is required on {tableDisplayName}."
                    };
                }
            }
        }

        return new Response<object>()
        {
            Succeeded = true,
        };
    }
}