using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;

namespace Xams.Core.Pipeline.Stages.Shared;

public static class PipelineUtil
{
    public static async Task<Response<object?>> SetExistingEntity(PipelineContext context, Guid id)
    {
        // Get the record from the database to check its *current* owning team\user
        Response<object?> readResponse = await context.DataRepository.Find(context.TableName!, id, true);

        if (!readResponse.Succeeded || readResponse.Data == null || readResponse.Data == null)
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = $"Could not find the record with id {id} in the {context.TableName} table."
            };
        }

        context.PreEntity = readResponse.Data;

        return readResponse;
    }
}