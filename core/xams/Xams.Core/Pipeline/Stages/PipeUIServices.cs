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
}