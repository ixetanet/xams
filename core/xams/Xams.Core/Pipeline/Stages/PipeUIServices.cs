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

        response = NumberRange(context);
        if (!response.Succeeded)
        {
            return response;
        }
        
        return await base.Execute(context);
    }

    private Response<object?> CharacterLimit(PipelineContext context)
    {
        var properties = context.Entity?.GetType().GetProperties();
        if (properties != null)
        {
            foreach (var property in properties)
            {
                var limitAttribute = property.GetCustomAttributes(typeof(UICharacterLimitAttribute), true);
                if (limitAttribute.Length > 0)
                {
                    var value = property.GetValue(context.Entity);
                    if (value != null)
                    {
                        var limit = ((UICharacterLimitAttribute)limitAttribute[0]).Limit;
                        if (value.ToString()!.Length > limit)
                        {
                            // Get the display name of the property
                            UIDisplayNameAttribute? displayNameAttribute =
                                property.GetCustomAttribute(typeof(UIDisplayNameAttribute), true) as
                                    UIDisplayNameAttribute;
                            return new Response<object?>()
                            {
                                Succeeded = false,
                                FriendlyMessage =
                                    $"The value for {displayNameAttribute?.Name ?? property.Name} exceeds the character limit of {limit}."
                            };
                        }
                    }
                }
            }
        }

        return new Response<object?>()
        {
            Succeeded = true
        };
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
        
        foreach (var property in context.Entity.GetType().GetEntityProperties())
        {
            if (property.GetCustomAttribute(typeof(UIRequiredAttribute), true) != null)
            {
                var value = property.GetValue(context.Entity);
                if (value == null || 
                    (property.PropertyType == typeof(string) && 
                     (string.IsNullOrEmpty(value.ToString()) || string.IsNullOrWhiteSpace(value.ToString()))))
                {
                    UIDisplayNameAttribute? displayNameAttribute =
                        property.GetCustomAttribute(typeof(UIDisplayNameAttribute), true) as UIDisplayNameAttribute;
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = $"{displayNameAttribute?.Name ?? property.Name} is required."
                    };
                }
            }
        }
        return new Response<object?>()
        {
            Succeeded = true
        };
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
}