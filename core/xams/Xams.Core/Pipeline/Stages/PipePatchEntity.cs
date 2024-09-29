using System.Reflection;
using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipePatchEntity : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        // If this is coming from the api, we need to patch the entity with the pre-entity
        PatchEntity(context);
        
        return await base.Execute(context);
    }
    
    private void PatchEntity(PipelineContext context)
    {
        if (context.PreEntity == null)
        {
            return;
        }

        // If there are fields this is being called from the api
        if (context.Fields == null)
        {
            return;
        }
        
        if (context.Entity == null)
        {
            return;
        }
        
        foreach (PropertyInfo property in context.PreEntity.GetType().GetEntityProperties())
        {
            if (!context.Fields.ContainsKey(property.Name))
            {
                object? oldValue = context.PreEntity.GetType().GetProperty(property.Name)
                    ?.GetValue(context.PreEntity);
                context.Entity.GetType().GetProperty(property.Name)?.SetValue(context.Entity, oldValue);
            }
        }
    }

}