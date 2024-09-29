using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;

namespace Xams.Core.Pipeline.Stages;

public class PipePreValidation : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        if (context.UserId == Guid.Empty)
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = "User ID is required."
            };
        }

        if (context.Entity == null && context.DataOperation is not DataOperation.Read)
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = $"Cannot {context.DataOperation.ToString()} null entity."
            };
        }
        
        return await base.Execute(context);
    }
}