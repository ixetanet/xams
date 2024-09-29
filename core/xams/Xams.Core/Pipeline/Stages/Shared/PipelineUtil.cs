using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;

namespace Xams.Core.Pipeline.Stages.Shared;

public static class PipelineUtil
{
    public static async Task<Response<ReadOutput>> SetExistingEntity(PipelineContext context, Guid id)
    {
        // Get the record from the database to check its *current* owning team\user
        Response<ReadOutput> readResponse = await context.DataRepository.Find(context.TableName!, id, true);

        if (!readResponse.Succeeded || readResponse.Data == null || readResponse.Data.results.Count == 0)
        {
            return new Response<ReadOutput>()
            {
                Succeeded = false,
                FriendlyMessage = $"Could not find the record with id {id} in the {context.TableName} table."
            };
        }

        context.PreEntity = readResponse.Data?.results[0];

        return readResponse;
    }
}