using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Pipeline.Stages.Shared;

namespace Xams.Core.Pipeline.Stages;

public class PipeGetPreEntity : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        // This may have already been set by a bulk operation
        if (context.PreEntity == null)
        {
            Guid? entityId = (Guid?)(context.Entity?.GetType().GetProperty($"{context.TableName}Id")
                ?.GetValue(context.Entity));
            if (entityId == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Cannot {context.DataOperation.ToString()} entity with empty ID."
                };
            }

            var existingEntityResponse = await PipelineUtil.SetExistingEntity(context, (Guid)entityId);
            if (!existingEntityResponse.Succeeded)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = existingEntityResponse.FriendlyMessage
                };
            }
        }
        return await base.Execute(context);
    }
}