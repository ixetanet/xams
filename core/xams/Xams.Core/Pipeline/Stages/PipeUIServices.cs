using System.Reflection;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipeUIServices : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var response = CharacterLimit(context);
        if (!response.Succeeded)
        {
            return response;
        }

        response = RequiredFields(context);
        if (!response.Succeeded)
        {
            return response;
        }

        response = ReadOnlyFields(context);
        if (!response.Succeeded)
        {
            return response;
        }

        response = CreateOnlyFields(context);
        if (!response.Succeeded)
        {
            return response;
        }

        response = NumberRange(context);
        if (!response.Succeeded)
        {
            return response;
        }

        return await base.Execute(context);
    }

    private Response<object?> CharacterLimit(PipelineContext context)
    {
        if (context.Entity == null)
        {
            return ServiceResult.Success();
        }

        // Use cached metadata instead of reflection
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var characterLimitFields = metadata.MetadataOutput.fields
            .Where(f => f.characterLimit.HasValue)
            .ToList();

        foreach (var field in characterLimitFields)
        {
            var property = context.Entity.GetType().GetProperty(field.name);
            if (property != null)
            {
                var value = property.GetValue(context.Entity);
                if (value != null && value.ToString()!.Length > field.characterLimit!.Value)
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"The value for {field.displayName ?? field.name} exceeds the character limit of {field.characterLimit}."
                    };
                }
            }
        }

        return ServiceResult.Success();
    }

    private Response<object?> RequiredFields(PipelineContext context)
    {
        if (context.Entity == null)
        {
            return new Response<object?>()
            {
                Succeeded = true,
                FriendlyMessage = "Entity is null."
            };
        }

        if (context.DataOperation is not (DataOperation.Create or DataOperation.Update))
        {
            return ServiceResult.Success();
        }

        // If no fields were provided (not from API), allow it
        if (context.Fields == null)
        {
            return ServiceResult.Success();
        }

        // Use cached metadata instead of reflection
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var requiredFields = metadata.MetadataOutput.fields
            .Where(f => f.isRequired)
            .ToList();

        foreach (var field in requiredFields)
        {
            var property = context.Entity.GetType().GetProperty(field.name);
            if (property != null)
            {
                var value = property.GetValue(context.Entity);
                if (value == null ||
                    (property.PropertyType == typeof(string) &&
                     (string.IsNullOrEmpty(value.ToString()) || string.IsNullOrWhiteSpace(value.ToString()))))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"{field.displayName ?? field.name} is required."
                    };
                }
            }
        }

        return ServiceResult.Success();
    }

    private Response<object?> NumberRange(PipelineContext context)
    {
        if (context.Entity == null)
        {
            return new Response<object?>()
            {
                Succeeded = true,
                FriendlyMessage = "Entity is null."
            };
        }

        if (context.DataOperation is not (DataOperation.Create or DataOperation.Update))
        {
            return ServiceResult.Success();
        }

        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var numberRangeFields = metadata.MetadataOutput.fields.Where(f => f.numberRange != null);

        foreach (var numberRangeField in numberRangeFields)
        {
            float min = float.Parse(numberRangeField.numberRange?.Split('-')[0] ?? throw new InvalidOperationException());
            float max = float.Parse(numberRangeField.numberRange?.Split('-')[1] ?? throw new InvalidOperationException());
            var property = context.Entity.GetType().GetProperty(numberRangeField.name);
            var value = property?.GetValue(context.Entity);
            if (property != null && value != null)
            {
                var displayName = numberRangeField.displayName ?? property.Name;
                var fieldPropertyType = property.PropertyType;
                fieldPropertyType = Nullable.GetUnderlyingType(fieldPropertyType) ?? fieldPropertyType;
                if (fieldPropertyType == typeof(int))
                {
                    if ((int)value < min || (int)value > max)
                    {
                        return ServiceResult.Error($"{displayName} is out of range.");
                    }
                }
                else if (fieldPropertyType == typeof(float))
                {
                    if ((float)value < min || (float)value > max)
                    {
                        return ServiceResult.Error($"{displayName} is out of range.");
                    }
                }
                else if (fieldPropertyType == typeof(double))
                {
                    if ((double)value < min || (double)value > max)
                    {
                        return ServiceResult.Error($"{displayName} is out of range.");
                    }
                }
                else if (fieldPropertyType == typeof(decimal))
                {
                    if ((decimal)value < (decimal)min || (decimal)value > (decimal)max)
                    {
                        return ServiceResult.Error($"{displayName} is out of range.");
                    }
                }
                else if (fieldPropertyType == typeof(long))
                {
                    if ((long)value < min || (long)value > max)
                    {
                        return ServiceResult.Error($"{displayName} is out of range.");
                    }
                }
            }
        }
        
        return new Response<object?>()
        {
            Succeeded = true
        };
    }

    private Response<object?> CreateOnlyFields(PipelineContext context)
    {
        if (context.DataOperation != DataOperation.Update)
        {
            return ServiceResult.Success();
        }

        if (context.Entity == null || context.PreEntity == null)
        {
            return ServiceResult.Success();
        }

        // If no fields were provided (not from API), allow it
        if (context.Fields == null)
        {
            return ServiceResult.Success();
        }

        // Use cached metadata instead of reflection
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var createOnlyFields = metadata.MetadataOutput.fields.Where(f => f.isCreateOnly).ToList();

        foreach (var field in createOnlyFields)
        {
            var property = context.Entity.GetType().GetProperty(field.name);
            if (property != null)
            {
                var currentValue = property.GetValue(context.Entity);
                var preValue = property.GetValue(context.PreEntity);

                // Check if value changed
                if (!Equals(currentValue, preValue))
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"{field.displayName ?? field.name} cannot be modified after creation."
                    };
                }
            }
        }

        return ServiceResult.Success();
    }

    private Response<object?> ReadOnlyFields(PipelineContext context)
    {
        if (context.DataOperation is not (DataOperation.Create or DataOperation.Update))
        {
            return ServiceResult.Success();
        }

        // If no fields were provided (not from API), allow it
        if (context.Fields == null)
        {
            return ServiceResult.Success();
        }

        if (context.Entity == null)
        {
            return ServiceResult.Success();
        }

        // Use cached metadata instead of reflection
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var readOnlyFields = metadata.MetadataOutput.fields
            .Where(f => f.isReadOnly)
            .ToList();

        foreach (var field in readOnlyFields)
        {
            // Check if user attempted to set this readonly field via API
            if (context.Fields.ContainsKey(field.name))
            {
                var property = context.Entity.GetType().GetProperty(field.name);
                if (property != null)
                {
                    var currentValue = property.GetValue(context.Entity);

                    if (currentValue == null)
                    {
                        continue;
                    }

                    // For Create operations, always block setting readonly fields
                    if (context.DataOperation == DataOperation.Create)
                    {
                        if (field.name == nameof(BaseEntity.OwningUserId) && (Guid?)currentValue == context.UserId)
                        {
                            continue;
                        }
                        
                        return new Response<object?>()
                        {
                            Succeeded = false,
                            FriendlyMessage = $"{field.displayName ?? field.name} is read-only and cannot be set."
                        };
                    }

                    // For Update operations, only block if value has changed
                    if (context.DataOperation == DataOperation.Update && context.PreEntity != null)
                    {
                        var preValue = property.GetValue(context.PreEntity);

                        // Check if value changed
                        if (!Equals(currentValue, preValue))
                        {
                            return new Response<object?>()
                            {
                                Succeeded = false,
                                FriendlyMessage = $"{field.displayName ?? field.name} is read-only and cannot be modified."
                            };
                        }
                    }
                }
            }
        }

        return ServiceResult.Success();
    }
}