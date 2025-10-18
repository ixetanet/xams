using Xams.Core.Base;
using Xams.Core.Dtos;

namespace Xams.Core.Pipeline.Stages;

public class PipeResultReadOutput : BasePipelineStage
{
    public override Task<Response<object?>> Execute(PipelineContext context)
    {
        // Remove Owner fields if they weren't requested
        bool removeOwningUserField = !context.ReadInput!.fields.Contains("*") && !context.ReadInput.fields.Contains("OwningUserId");
        bool removeOwningTeamField = !context.ReadInput!.fields.Contains("*") && !context.ReadInput.fields.Contains("OwningTeamId");

        // Get custom owning user fields that should be removed
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);
        var owningUserFieldsToRemove = metadata.OwningUserFields
            .Where(field => !context.ReadInput!.fields.Contains("*") && !context.ReadInput.fields.Contains(field))
            .ToList();

        foreach (var result in context.ReadOutput!.results)
        {
            var dict = (IDictionary<string, object>)result;
            if (removeOwningUserField && dict.ContainsKey("OwningUserId"))
            {
                dict.Remove("OwningUserId");
            }
            if (removeOwningTeamField && dict.ContainsKey("OwningTeamId"))
            {
                dict.Remove("OwningTeamId");
            }

            // Remove custom owning user fields
            foreach (var owningUserField in owningUserFieldsToRemove)
            {
                if (dict.ContainsKey(owningUserField))
                {
                    dict.Remove(owningUserField);
                }
            }
        }

        context.ReadOutput.parameters = context.OutputParameters;

        return Task.FromResult(new Response<object?>()
        {
            Succeeded = true,
            Data =  context.ReadOutput
        });
    }
}